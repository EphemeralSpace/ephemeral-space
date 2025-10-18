using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._ES.Auditions;
using Content.Server._ES.Masks.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Shared._ES.Masks;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Verbs;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Masks;

public sealed class ESMaskSystem : ESSharedMaskSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ESAuditionsSystem _esAuditions = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly JobSystem _job = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly RoleSystem _role = default!;

    private static readonly EntProtoId<ESMaskRoleComponent> MindRole = "ESMindRoleMask";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESTroupeRuleComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnRulePlayerJobsAssigned);
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(GetVerbs);
    }

    private void OnMapInit(Entity<ESTroupeRuleComponent> ent, ref MapInitEvent args)
    {
        var troupe = PrototypeManager.Index(ent.Comp.Troupe);
        var objectives = _entityTable.GetSpawns(troupe.Objectives);

        var dummyMind = _mind.CreateMind(null);

        foreach (var objective in objectives)
        {
            if (!_objectives.TryCreateObjective(dummyMind, objective, out var objectiveUid))
                continue;
            ent.Comp.AssociatedObjectives.Add(objectiveUid.Value);
        }
        Del(dummyMind);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!ev.LateJoin)
            return;
        AssignPlayersToTroupe([ev.Player]);
    }

    private void OnRulePlayerJobsAssigned(RulePlayerJobsAssignedEvent args)
    {
        AssignPlayersToTroupe(args.Players.ToList());
    }

    private void GetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!AdminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        if (!HasComp<MindContainerComponent>(args.Target) ||
            !TryComp<ActorComponent>(args.Target, out var actorComp))
            return;

        foreach (var mask in PrototypeManager.EnumeratePrototypes<ESMaskPrototype>())
        {
            if (mask.Abstract)
                continue;

            var verb = new Verb
            {
                Category = ESMask,
                Text = Loc.GetString("es-verb-apply-mask-name",
                    ("name", Loc.GetString(mask.Name)),
                    ("troupe", Loc.GetString(PrototypeManager.Index(mask.Troupe).Name))),
                Message = Loc.GetString("es-verb-apply-mask-desc", ("mask", Loc.GetString(mask.Name))),
                Priority = HashCode.Combine(mask.Troupe, mask.Name),
                ConfirmationPopup = true,
                Act = () =>
                {
                    if (!_mind.TryGetMind(actorComp.PlayerSession, out var mind, out var mindComp))
                        return;
                    // TODO: We may need to associate these with a troupe rule ent in the future.
                    // For now, this is just for testing and doesn't need to necessarily support everything
                    // In a future ideal implementation, every troupe should have an associated "minimum viable rule"
                    // such that if a given troupe does not have a corresponding rule, one can be created.
                    ApplyMask((mind, mindComp), mask, null);
                },
            };
            args.Verbs.Add(verb);
        }
    }

    public void AssignPlayersToTroupe(List<ICommonSession> players)
    {
        foreach (var troupe in GetOrderedTroupes())
        {
            if (players.Count == 0)
                break;
            TryAssignToTroupe(troupe, ref players);
        }

        if (players.Count > 0)
        {
            Log.Warning($"Failed to assign all players to troupes! Leftover count: {players.Count}");
        }
    }

    public bool TryAssignToTroupe(Entity<ESTroupeRuleComponent> ent, ref List<ICommonSession> players)
    {
        var troupe = PrototypeManager.Index(ent.Comp.Troupe);

        var filteredPlayers = players.Where(s => IsPlayerValid(troupe, s)).ToList();

        var playerCount = _esAuditions.GetPlayerCount();
        var targetCount = Math.Clamp(playerCount / ent.Comp.PlayersPerTargetMember, ent.Comp.MinTargetMembers, ent.Comp.MaxTargetMembers);
        var targetDiff = Math.Min(targetCount - ent.Comp.TroupeMemberMinds.Count, filteredPlayers.Count);
        if (targetDiff <= 0)
            return false;

        for (var i = 0; i < targetDiff; i++)
        {
            var player = _random.PickAndTake(filteredPlayers);
            players.Remove(player);

            if (!_mind.TryGetMind(player, out var mind, out var mindComp))
            {
                Log.Warning($"Failed to get mind for session {player}");
                continue;
            }

            if (!TryGetMask((mind, mindComp), troupe, out var mask))
            {
                Log.Warning($"Failed to get mask for session {player} on troupe {troupe.ID} ({ToPrettyString(ent)}");
                continue;
            }

            ApplyMask((mind, mindComp), mask.Value, ent);
        }
        return true;
    }

    public List<Entity<ESTroupeRuleComponent>> GetOrderedTroupes()
    {
        var troupes = new List<Entity<ESTroupeRuleComponent>>();
        var query = EntityQueryEnumerator<ESTroupeRuleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_gameTicker.IsGameRuleActive(uid))
                continue;

            troupes.Add((uid, comp));
        }

        troupes.Sort((a, b) =>
        {
            var c = a.Comp.Priority.CompareTo(b.Comp.Priority);
            if (c != 0)
                return c;
            return _random.Next() % 2 == 0 ? -1 : 1; // For members of equal priority, sort them randomly.
        });

        return troupes;
    }

    public bool IsPlayerValid(ESTroupePrototype troupe, ICommonSession player)
    {
        if (!_mind.TryGetMind(player, out var mind, out _))
            return false;

        // BUG: MindTryGetJobId doesn't have a NotNullWhen attribute on the out param.
        if (_job.MindTryGetJobId(mind, out var job) && troupe.ProhibitedJobs.Contains(job!.Value))
            return false;

        if (player.AttachedEntity is null)
            return false;

        return true;
    }

    public bool TryGetMask(Entity<MindComponent> mind, ESTroupePrototype troupe, [NotNullWhen(true)] out ProtoId<ESMaskPrototype>? mask)
    {
        mask = null;

        var weights = new Dictionary<ESMaskPrototype, float>();
        foreach (var maskProto in PrototypeManager.EnumeratePrototypes<ESMaskPrototype>())
        {
            if (maskProto.Abstract)
                continue;

            if (maskProto.Troupe != troupe)
                continue;

            weights.Add(maskProto, maskProto.Weight);
        }

        if (weights.Count == 0)
            return false;

        mask = _random.Pick(weights);
        return true;
    }

    public void ApplyMask(Entity<MindComponent> mind, ProtoId<ESMaskPrototype> maskId, Entity<ESTroupeRuleComponent>? troupe)
    {
        var mask = PrototypeManager.Index(maskId);

        _role.MindAddRole(mind, MindRole, mind, true);

        var objectives = _entityTable.GetSpawns(mask.Objectives);
        foreach (var objective in objectives)
        {
            _mind.TryAddObjective(mind, mind, objective);
        }

        var msg = Loc.GetString("es-mask-selected-chat-message",
            ("role", Loc.GetString(mask.Name)),
            ("description", Loc.GetString(mask.Description)));

        if (mind.Comp.UserId is { } userId && _player.TryGetSessionById(userId, out var session))
        {
            _chat.ChatMessageToOne(ChatChannel.Server, msg, msg, default, false, session.Channel, Color.Plum);
        }

        if (mind.Comp.OwnedEntity is { } ownedEntity)
            EntityManager.AddComponents(ownedEntity, mask.Components);
        EntityManager.AddComponents(mind, mask.MindComponents);

        // TODO: eventually, this should be required.
        if (troupe == null)
            return;

        troupe.Value.Comp.TroupeMemberMinds.Add(mind);

        foreach (var objective in troupe.Value.Comp.AssociatedObjectives)
        {
            _mind.AddObjective(mind, mind, objective);
        }
    }
}
