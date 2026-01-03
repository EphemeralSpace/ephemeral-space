using System.Linq;
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
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Roles.Components;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks;

public sealed class ESMaskSystem : ESSharedMaskSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly JobSystem _job = default!;

    private static readonly EntProtoId<ESMaskRoleComponent> MindRole = "ESMindRoleMask";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextAppend);

        SubscribeLocalEvent<ESTroupeRuleComponent, GameRuleStartedEvent>(OnGameRuleStarted);

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

    private void OnGameRuleStarted(Entity<ESTroupeRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        if (_gameTicker.RunLevel == GameRunLevel.InRound)
            InitializeTroupeObjectives(ent);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!ev.LateJoin)
            return;

        var ev2 = new AssignLatejoinerToTroupeEvent(false, ev.Player);
        RaiseLocalEvent(ref ev2);
    }

    private void OnRulePlayerJobsAssigned(RulePlayerJobsAssignedEvent args)
    {
        AssignPlayersToTroupe(args.Players.ToList());
        InitializeTroupeObjectives();
    }

    public void AssignPlayersToTroupe(List<ICommonSession> players)
    {
        var ev = new AssignPlayersToTroupeEvent(false, players);
        RaiseLocalEvent(ref ev);
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

    public override void ApplyMask(Entity<MindComponent> mind, ProtoId<ESMaskPrototype> maskId, Entity<ESTroupeRuleComponent>? troupe = null)
    {
        var mask = PrototypeManager.Index(maskId);

        // If we are spawning a new rule, we should initialize the objectives *after*
        // the first player is added to ensure targeting shenanigans don't happen.
        var ruleExists = troupe.HasValue;
        if (troupe is null && !TryGetTroupeEntityForMask(mask, out troupe))
        {
            var troupeEnt = _gameTicker.AddGameRule(PrototypeManager.Index(mask.Troupe).GameRule);
            troupe = (troupeEnt, Comp<ESTroupeRuleComponent>(troupeEnt));
        }

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

        if (_player.TryGetSessionById(mind.Comp.UserId, out var session))
        {
            _chat.ChatMessageToOne(ChatChannel.Server, msg, msg, default, false, session.Channel, Color.Plum);
        }

        if (mind.Comp.OwnedEntity is { } ownedEntity)
        {
            EntityManager.SpawnInBag(_entityTable.GetSpawns(mask.Gear), ownedEntity);
            EntityManager.AddComponents(ownedEntity, mask.Components);
        }
        EntityManager.AddComponents(mind, mask.MindComponents);

        troupe.Value.Comp.TroupeMemberMinds.Add(mind);
        Objective.RegenerateObjectiveList(mind.Owner);

        // Our rule was only added in the beginning, now we should start it properly.
        if (!ruleExists)
            _gameTicker.StartGameRule(troupe.Value);
    }

    public override void RemoveMask(Entity<MindComponent> mind)
    {
        if (!TryGetMask(mind.AsNullable(), out var maskId))
            return;

        var mask = PrototypeManager.Index(maskId);

        Role.MindRemoveRole(mind!, new EntProtoId<MindRoleComponent>(MindRole));

        if (mind.Comp.OwnedEntity is { } ownedEntity)
        {
            EntityManager.RemoveComponents(ownedEntity, mask.Components);
        }
        EntityManager.RemoveComponents(mind, mask.MindComponents);

        foreach (var objective in Objective.GetOwnedObjectives<ESMaskObjectiveComponent>(mind.Owner))
        {
            Objective.TryRemoveObjective(mind.Owner, objective.Owner);
        }

        if (TryGetTroupeEntity(mask.Troupe, out var troupeEntity))
        {
            troupeEntity.Value.Comp.TroupeMemberMinds.Remove(mind);
        }

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
