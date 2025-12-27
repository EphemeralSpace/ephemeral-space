using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._ES.Auditions;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Roles.Jobs;
using Content.Shared._ES.Auditions.Components;
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

    private static readonly EntProtoId<ESMaskRoleComponent> MindRole = "ESMindRoleMask";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextAppend);

        SubscribeLocalEvent<ESTroupeRuleComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnRulePlayerJobsAssigned);
    }

    private void OnRoundEndTextAppend(RoundEndTextAppendEvent ev)
    {
        var troupes = GetOrderedTroupes();

        ev.AddLine(Loc.GetString("es-roundend-mask-count-troupe"));
        foreach (var troupe in troupes)
        {
            var troupeProto = PrototypeManager.Index(troupe.Comp.Troupe);
            ev.AddLine(Loc.GetString("es-roundend-mask-troupe-list",
                ("name", Loc.GetString(troupeProto.Name)),
                ("color", troupeProto.Color)));
            foreach (var objective in Objective.GetObjectives(troupe.Owner))
            {
                ev.AddLine(Loc.GetString("es-roundend-mask-objective-fmt",
                    ("text", Objective.GetObjectiveString(objective.AsNullable()))));
            }
        }

        ev.AddLine(string.Empty);
        ev.AddLine(Loc.GetString("es-roundend-mask-player-summary-header"));
        foreach (var troupe in troupes)
        {
            var troupeProto = PrototypeManager.Index(troupe.Comp.Troupe);

            ev.AddLine(Loc.GetString("es-roundend-mask-player-group",
                ("name", Loc.GetString(troupeProto.Name)),
                ("color", troupeProto.Color)));
            foreach (var mind in troupe.Comp.TroupeMemberMinds)
            {
                if (!TryComp<MindComponent>(mind, out var mindComp) ||
                    !TryComp<ESCharacterComponent>(mind, out var character))
                    continue;

                var username = mindComp.OriginalOwnerUserId != null
                    ? _player.GetPlayerData(mindComp.OriginalOwnerUserId.Value).UserName
                    : Loc.GetString("generic-unknown-title");

                var maskName = TryGetMask((mind, mindComp), out var mask)
                    ? Loc.GetString(PrototypeManager.Index(mask.Value).Name)
                    : Loc.GetString("generic-unknown-title");

                var maskColor = mask == null
                    ? Color.White
                    : PrototypeManager.Index(mask).Color;

                // get mask-specific objectives
                var objectives = Objective.GetObjectives(mind)
                    .Except(Objective.GetObjectives(troupe.Owner))
                    .ToList();

                ev.AddLine(Loc.GetString("es-roundend-mask-player-summary",
                    ("name", character.Name),
                    ("username", username),
                    ("maskColor", maskColor),
                    ("maskName", maskName),
                    ("objCount", objectives.Count)));

                foreach (var objective in objectives)
                {
                    ev.AddLine(Loc.GetString("es-roundend-mask-objective-fmt",
                        ("text", Objective.GetObjectiveString(objective.AsNullable()))));
                }
            }
            ev.AddLine(string.Empty);
        }
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

        // TODO: Refactor this to not simply fall back to random selection logic.
        // All of this logic should probably live in MasqueradeKind, and be reworked
        // to prefer ensuring a balanced set of masks over potentially compromising
        // due to too many command players for all the traitors to be assigned.
        // The entire random selection thing should be moved to RandomMasquerade,
        // and a general API should be added to MasqueradeKind for getting masks for
        // players.
        var ev2 = new AssignLatejoinerToTroupeEvent(false, ev.Player);
        RaiseLocalEvent(ref ev2);

        if (!ev2.Handled)
            AssignPlayersToTroupe([ev.Player]);
    }

    private void OnRulePlayerJobsAssigned(RulePlayerJobsAssignedEvent args)
    {
        AssignPlayersToTroupe(args.Players.ToList());
        InitializeTroupeObjectives();
    }

    public void AssignPlayersToTroupe(List<ICommonSession> players)
    {
        // TODO: See comment in OnPlayerSpawnComplete, this needs refactored.
        // but I don't want to change and test the existing logic for an already
        // massive PR that blocks others' work.

        var ev = new AssignPlayersToTroupeEvent(false, players);
        RaiseLocalEvent(ref ev);

        if (!ev.Handled)
        {
            var playerCount = players.Count;

            Log.Info("Nobody handled player assignment, doing it randomly.");
            foreach (var troupe in GetOrderedTroupes())
            {
                if (players.Count == 0)
                    break;

                TryAssignToTroupe(troupe, ref players, playerCount);
            }
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
        var troupe = PrototypeManager.Index(rule.Comp.Troupe);
        Objective.TryAddObjective(rule.Owner, troupe.Objectives);
    }

    public bool TryAssignToTroupe(Entity<ESTroupeRuleComponent> ent, ref List<ICommonSession> players, int playerCount)
    {
        var troupe = PrototypeManager.Index(ent.Comp.Troupe);

        var filteredPlayers = players.Where(s => IsPlayerValid(troupe, s)).ToList();

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

            // TODO: check the mask has valid objectives.
            // Don't assign masks if their objectives can't be done.

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

        Objective.TryAddObjective(mind.Owner, mask.Objectives);

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
        Objective.RegenerateObjectiveList(mind.Owner);
    }
}

/// <summary>
///     Fired when players are being assigned to a troupe. Old random assignment algorithm kicks in
///     if not handled. (This is a mild hack.)
/// </summary>
[ByRefEvent]
public record struct AssignPlayersToTroupeEvent(bool Handled, List<ICommonSession> Players);

/// <summary>
///     Fired when players are latejoining. Old random assignment algorithm kicks in
///     if not handled. (This is a mild hack.)
/// </summary>
[ByRefEvent]
public record struct AssignLatejoinerToTroupeEvent(bool Handled, ICommonSession Victim);
