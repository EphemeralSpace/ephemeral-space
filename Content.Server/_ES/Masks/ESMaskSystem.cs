using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._ES.Auditions;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Objectives;
using Content.Server.Roles.Jobs;
using Content.Shared._ES.Core.Entity;
using Content.Shared._ES.Masks;
using Content.Shared._ES.Masks.Components;
using Content.Shared.Chat;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Random.Helpers;
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
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    private static readonly EntProtoId<ESMaskRoleComponent> MindRole = "ESMindRoleMask";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESTroupeRuleComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnRulePlayerJobsAssigned);
    }

    private void OnMapInit(Entity<ESTroupeRuleComponent> ent, ref MapInitEvent args)
    {
        if (_gameTicker.RunLevel == GameRunLevel.InRound)
            InitializeTroupeObjectives(ent);
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
        InitializeTroupeObjectives();
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

    public void InitializeTroupeObjectives()
    {
        var query = EntityQueryEnumerator<ESTroupeRuleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            InitializeTroupeObjectives((uid, comp));
        }
    }

    public void InitializeTroupeObjectives(Entity<ESTroupeRuleComponent> rule)
    {
        var (uid, comp) = rule;
        var troupe = PrototypeManager.Index(comp.Troupe);
        var objectives = _entityTable.GetSpawns(troupe.Objectives).ToList();
        if (objectives.Count == 0)
            return;
        // Yes. This sucks. Yell at me when objectives aren't dogshit
        EnsureComp<MindComponent>(uid);

        var dummyMind = Mind.CreateMind(null);

        foreach (var objective in objectives)
        {
            if (!_objectives.TryCreateObjective(dummyMind, objective, out var objectiveUid))
                continue;
            comp.AssociatedObjectives.Add(objectiveUid.Value);
            foreach (var mind in comp.TroupeMemberMinds)
            {
                var mindComp = Comp<MindComponent>(mind);
                Mind.AddObjective(mind, mindComp, objectiveUid.Value);
            }
        }
        Del(dummyMind);
    }

    public bool TryAssignToTroupe(Entity<ESTroupeRuleComponent> ent, ref List<ICommonSession> players)
    {
        var troupe = PrototypeManager.Index(ent.Comp.Troupe);

        var filteredPlayers = players.Where(s => IsPlayerValid(troupe, s)).ToList();

        var playerCount = _esAuditions.GetPlayerCount();
        var targetCount = Math.Clamp((int)MathF.Ceiling((float) playerCount / ent.Comp.PlayersPerTargetMember), ent.Comp.MinTargetMembers, ent.Comp.MaxTargetMembers);
        var targetDiff = Math.Min(targetCount - ent.Comp.TroupeMemberMinds.Count, filteredPlayers.Count);
        if (targetDiff <= 0)
            return false;

        for (var i = 0; i < targetDiff; i++)
        {
            var player = _random.PickAndTake(filteredPlayers);
            players.Remove(player);

            if (!Mind.TryGetMind(player, out var mind, out var mindComp))
            {
                Log.Warning($"Failed to get mind for session {player}");
                continue;
            }

            if (!TryGetAssignableMaskFromTroupe((mind, mindComp), troupe, out var mask))
            {
                Log.Warning($"Failed to get mask for session {player} on troupe {troupe.ID} ({ToPrettyString(ent)}");
                continue;
            }

            ApplyMask((mind, mindComp), mask.Value, ent);
        }
        return true;
    }

    public bool IsPlayerValid(ESTroupePrototype troupe, ICommonSession player)
    {
        if (!Mind.TryGetMind(player, out var mind, out _))
            return false;

        // BUG: MindTryGetJobId doesn't have a NotNullWhen attribute on the out param.
        if (_job.MindTryGetJobId(mind, out var job) && troupe.ProhibitedJobs.Contains(job!.Value))
            return false;

        if (player.AttachedEntity is null)
            return false;

        return true;
    }

    public bool TryGetAssignableMaskFromTroupe(Entity<MindComponent> mind, ESTroupePrototype troupe, [NotNullWhen(true)] out ProtoId<ESMaskPrototype>? mask)
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

    public override void ApplyMask(Entity<MindComponent> mind, ProtoId<ESMaskPrototype> maskId, Entity<ESTroupeRuleComponent> troupe)
    {
        var mask = PrototypeManager.Index(maskId);

        // Only exists because the AddRole API does not return the newly added role (why???)
        Role.MindAddRole(mind, MindRole, mind, true);
        if (!Role.MindHasRole<ESMaskRoleComponent>(mind.AsNullable(), out var role))
            throw new Exception($"Failed to add mind role to {Mind.MindOwnerLoggingString(mind)} for mask {maskId}");
        var roleComp = role.Value.Comp2;
        roleComp.Mask = maskId;
        Dirty(role.Value, roleComp);

        foreach (var objective in _entityTable.GetSpawns(mask.Objectives))
        {
            Mind.TryAddObjective(mind, mind, objective);
        }

        var msg = Loc.GetString("es-mask-selected-chat-message",
            ("role", Loc.GetString(mask.Name)),
            ("description", Loc.GetString(mask.Description)));

        if (mind.Comp.UserId is { } userId && _player.TryGetSessionById(userId, out var session))
        {
            _chat.ChatMessageToOne(ChatChannel.Server, msg, msg, default, false, session.Channel, Color.Plum);
        }

        if (mind.Comp.OwnedEntity is { } ownedEntity)
        {
            EntityManager.SpawnInBag(_entityTable.GetSpawns(mask.Gear), ownedEntity);
            EntityManager.AddComponents(ownedEntity, mask.Components);
        }
        EntityManager.AddComponents(mind, mask.MindComponents);

        troupe.Comp.TroupeMemberMinds.Add(mind);
        foreach (var objective in troupe.Comp.AssociatedObjectives)
        {
            Mind.AddObjective(mind, mind, objective);
        }
    }
}
