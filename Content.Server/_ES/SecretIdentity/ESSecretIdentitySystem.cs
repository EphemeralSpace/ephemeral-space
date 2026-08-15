using System.Linq;
using Content.Server._ES.Stagehand;
using Content.Server.Actions;
using Content.Server.GameTicking;
using Content.Server.Roles.Jobs;
using Content.Server.Station.Systems;
using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.Chat;
using Content.Shared._ES.SecretIdentity;
using Content.Shared._ES.SecretIdentity.Components;
using Content.Shared._ES.Stagehand;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Roles.Components;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._ES.SecretIdentity;

public sealed partial class ESSecretIdentitySystem : ESSharedSecretIdentitySystem
{
    [Dependency] private IESSharedChatManager _chat = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private JobSystem _job = default!;
    [Dependency] private ESStagehandNotificationsSystem _stagehandNotifications = default!;
    [Dependency] private StationSpawningSystem _stationSpawning = default!;

    private static readonly EntProtoId<ESSecretIdentityRoleComponent> MindRole = "ESMindRoleSecretIdentity";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextAppend);

        SubscribeLocalEvent<ESOrganizationRuleComponent, GameRuleStartedEvent>(OnGameRuleStarted);

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnRulePlayerJobsAssigned);
    }

    private void OnRoundEndTextAppend(RoundEndTextAppendEvent ev)
    {
        var organizations = GetOrderedOrganizations();

        ev.AddLine(Loc.GetString("es-roundend-secret-identity-count-organization"));
        foreach (var organization in organizations)
        {
            var organizationProto = PrototypeManager.Index(organization.Comp.Organization);
            ev.AddLine(Loc.GetString("es-roundend-secret-identity-organization-list",
                ("name", Loc.GetString(organizationProto.Name)),
                ("color", organizationProto.Color)));
            foreach (var objective in Objective.GetObjectives(organization.Owner))
            {
                ev.AddLine(Loc.GetString("es-roundend-secret-identity-objective-fmt",
                    ("text", Objective.GetObjectiveString(objective.AsNullable()))));
            }
        }

        ev.AddLine(string.Empty);
        ev.AddLine(Loc.GetString("es-roundend-secret-identity-player-summary-header"));
        foreach (var organization in organizations)
        {
            var organizationProto = PrototypeManager.Index(organization.Comp.Organization);

            ev.AddLine(Loc.GetString("es-roundend-secret-identity-player-group",
                ("name", Loc.GetString(organizationProto.Name)),
                ("color", organizationProto.Color)));
            foreach (var mind in organization.Comp.OrganizationMemberMinds)
            {
                if (!TryComp<MindComponent>(mind, out var mindComp) ||
                    !TryComp<ESCharacterComponent>(mind, out var character))
                    continue;

                var username = mindComp.OriginalOwnerUserId != null
                    ? _player.GetPlayerData(mindComp.OriginalOwnerUserId.Value).UserName
                    : Loc.GetString("generic-unknown-title");

                var secretIdentityName = GetSecretIdentityMemoryString(mind);

                // get secret-identity-specific objectives
                var objectives = Objective.GetObjectives(mind)
                    .Except(Objective.GetObjectives(organization.Owner))
                    .ToList();

                ev.AddLine(Loc.GetString("es-roundend-secret-identity-player-summary",
                    ("name", character.Name),
                    ("username", username),
                    ("secretIdentityName", secretIdentityName),
                    ("objCount", objectives.Count)));

                foreach (var objective in objectives)
                {
                    ev.AddLine(Loc.GetString("es-roundend-secret-identity-objective-fmt",
                        ("text", Objective.GetObjectiveString(objective.AsNullable()))));
                }
            }
            ev.AddLine(string.Empty);
        }
    }

    /// <summary>
    /// Formats all secret identities a mind has owned in the form {identity1}-turned-{identity2}-turned-{identity3} and so on.
    /// </summary>
    public string GetSecretIdentityMemoryString(Entity<ESSecretIdentityMemoryComponent?> mind)
    {
        if (!Resolve(mind, ref mind.Comp, false))
            return Loc.GetString("generic-unknown-title");

        // You should always have SOME identity
        DebugTools.Assert(mind.Comp.SecretIdentities.Count != 0);

        var firstSecretIdentity = PrototypeManager.Index(mind.Comp.SecretIdentities.First());

        var outString = Loc.GetString("es-roundend-secret-identity-fmt",
            ("name", Loc.GetString(firstSecretIdentity.Name)),
            ("color", firstSecretIdentity.Color));

        for (var i = 1; i < mind.Comp.SecretIdentities.Count; ++i)
        {
            var secretIdentity = PrototypeManager.Index(mind.Comp.SecretIdentities[i]);
            var secretIdentityString = Loc.GetString("es-roundend-secret-identity-fmt",
                ("name", Loc.GetString(secretIdentity.Name)),
                ("color", secretIdentity.Color));

            // Chain all the identities together.
            outString = Loc.GetString("es-roundend-secret-identity-link-fmt",
                ("secretIdentity1", outString),
                ("secretIdentity2", secretIdentityString));
        }

        return outString;
    }

    private void OnGameRuleStarted(Entity<ESOrganizationRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        if (_gameTicker.RunLevel == GameRunLevel.InRound)
            InitializeOrganizationObjectives(ent);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!ev.LateJoin)
            return;

        var ev2 = new AssignLatejoinerToOrganizationEvent(false, ev.Player);
        RaiseLocalEvent(ref ev2);
    }

    private void OnRulePlayerJobsAssigned(RulePlayerJobsAssignedEvent args)
    {
        AssignPlayersToOrganization(args.Players.ToList());
        InitializeOrganizationObjectives();
    }

    public void AssignPlayersToOrganization(List<ICommonSession> players)
    {
        var ev = new AssignPlayersToOrganizationEvent(false, players);
        RaiseLocalEvent(ref ev);
    }

    public void InitializeOrganizationObjectives()
    {
        var query = EntityQueryEnumerator<ESOrganizationRuleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            InitializeOrganizationObjectives((uid, comp));
        }
    }

    public void InitializeOrganizationObjectives(Entity<ESOrganizationRuleComponent> rule)
    {
        var organization = PrototypeManager.Index(rule.Comp.Organization);
        foreach (var objective in _entityTable.GetSpawns(organization.Objectives))
        {
            if (!Objective.TryAddObjective(rule.Owner, objective, out var objectiveUid))
                continue;

            Objective.SetDescriptor(
                objectiveUid.Value,
                Loc.GetString("es-objective-text-organization"),
                organization.Color,
                Loc.GetString("es-objective-tooltip-organization"));
        }
    }

    public bool IsPlayerValid(ESSecretIdentityPrototype secretIdentity, ICommonSession player)
    {
        if (!Mind.TryGetMind(player, out var mind, out _))
            return false;

        if (_job.MindTryGetJobId(mind, out var job) && secretIdentity.ProhibitedJobs.Contains(job.Value))
            return false;

        if (player.AttachedEntity is null)
            return false;

        return true;
    }

    public override void ApplySecretIdentity(Entity<MindComponent> mind, ProtoId<ESSecretIdentityPrototype> secretIdentityId, Entity<ESOrganizationRuleComponent>? organization = null)
    {
        var secretIdentity = PrototypeManager.Index(secretIdentityId);

        // If we are spawning a new rule, we should initialize the objectives *after*
        // the first player is added to ensure targeting shenanigans don't happen.
        var ruleExists = organization.HasValue;
        if (organization is null && !TryGetOrganizationEntityForSecretIdentity(secretIdentity, out organization))
        {
            var organizationEnt = _gameTicker.AddGameRule(PrototypeManager.Index(secretIdentity.Organization).GameRule);
            organization = (organizationEnt, Comp<ESOrganizationRuleComponent>(organizationEnt));
        }

        // Only exists because the AddRole API does not return the newly added role (why???)
        Role.MindAddRole(mind, MindRole, mind, true);
        if (!Role.MindHasRole<ESSecretIdentityRoleComponent>(mind.AsNullable(), out var role))
            throw new Exception($"Failed to add mind role to {Mind.MindOwnerLoggingString(mind)} for secret identity {secretIdentityId}");
        var roleComp = role.Value.Comp2;
        roleComp.SecretIdentity = secretIdentityId;
        Dirty(role.Value, roleComp);

        foreach (var objective in _entityTable.GetSpawns(secretIdentity.Objectives))
        {
            if (!Objective.TryAddObjective(mind.Owner, objective, out var objectiveUid))
                continue;

            Objective.SetDescriptor(
                objectiveUid.Value,
                Loc.GetString("es-objective-text-secret-identity"),
                secretIdentity.Color,
                Loc.GetString("es-objective-tooltip-secret-identity"));
        }

        var msg = Loc.GetString("es-secret-identity-selected-chat-message",
            ("role", Loc.GetString(secretIdentity.Name)),
            ("description", Loc.GetString(secretIdentity.Description)));

        if (_player.TryGetSessionById(mind.Comp.UserId, out var session))
        {
            _chat.SendServerMessage(msg, session, Color.Plum);
        }

        if (mind.Comp.OwnedEntity is { } ownedEntity)
        {
            _stationSpawning.EquipStartingGear(ownedEntity, secretIdentity.Gear);
            EntityManager.AddComponents(ownedEntity, secretIdentity.Components);
            EnsureComp<ESBodyLastSecretIdentityComponent>(ownedEntity).LastSecretIdentity = secretIdentity;

            // TODO: these should be tied to the mind, but OH MY GOD that code is ass.
            // Save that shit for another day.
            foreach (var action in _entityTable.GetSpawns(secretIdentity.Actions))
            {
                if (_actions.AddAction(ownedEntity, action) is { } actionEntity)
                    role.Value.Comp2.Actions.Add(actionEntity);
            }
        }
        EntityManager.AddComponents(mind, secretIdentity.MindComponents);

        var memoryComponent = EnsureComp<ESSecretIdentityMemoryComponent>(mind);
        memoryComponent.SecretIdentities.Add(secretIdentity);

        organization.Value.Comp.OrganizationMemberMinds.Add(mind);
        Objective.RegenerateObjectiveList(mind.Owner);

        // Our rule was only added in the beginning, now we should start it properly.
        if (!ruleExists)
            _gameTicker.StartGameRule(organization.Value);

        RefreshCharacterInfoBlurb(mind.AsNullable());

        var ev = new ESSecretIdentityChangedEvent(mind, secretIdentity, null);
        RaiseLocalEvent(organization.Value, ref ev, true);
    }

    public override void RemoveSecretIdentity(Entity<MindComponent> mind)
    {
        if (!TryGetSecretIdentity(mind.AsNullable(), out var secretIdentityId) ||
            !Role.MindHasRole<ESSecretIdentityRoleComponent>(mind.Owner, out var role))
            return;

        var secretIdentity = PrototypeManager.Index(secretIdentityId);

        if (mind.Comp.OwnedEntity is { } ownedEntity)
        {
            EntityManager.RemoveComponents(ownedEntity, secretIdentity.Components);
        }
        EntityManager.RemoveComponents(mind, secretIdentity.MindComponents);

        foreach (var action in role.Value.Comp2.Actions)
        {
            _actions.RemoveAction(action);
        }

        foreach (var objective in Objective.GetOwnedObjectives<ESSecretIdentityObjectiveComponent>(mind.Owner))
        {
            Objective.TryRemoveObjective(mind.Owner, objective.Owner);
        }

        if (TryGetOrganizationEntity(secretIdentity.Organization, out var organizationEntity))
        {
            organizationEntity.Value.Comp.OrganizationMemberMinds.Remove(mind);
        }

        Role.MindRemoveRole(mind.AsNullable(), new EntProtoId<MindRoleComponent>(MindRole));

        Objective.RegenerateObjectiveList(mind.Owner);
        RefreshCharacterInfoBlurb(mind.AsNullable());

        if (organizationEntity.HasValue)
        {
            var ev = new ESSecretIdentityChangedEvent(mind, null, secretIdentity);
            RaiseLocalEvent(organizationEntity.Value, ref ev, true);
        }
    }

    public override void ChangeSecretIdentity(Entity<MindComponent> mind,
        ProtoId<ESSecretIdentityPrototype> secretIdentityId,
        Entity<ESOrganizationRuleComponent>? organization = null,
        bool eraseHistory = false)
    {
        RemoveSecretIdentity(mind);
        if (eraseHistory)
        {
            var comp = EnsureComp<ESSecretIdentityMemoryComponent>(mind);
            if (comp.SecretIdentities.Count != 0)
                comp.SecretIdentities.RemoveAt(comp.SecretIdentities.Count - 1);
        }
        ApplySecretIdentity(mind, secretIdentityId, organization);

        if (!eraseHistory && mind.Comp.OwnedEntity is { } owned)
        {
            var msg = Loc.GetString("es-stagehand-notification-secret-identity-change",
                ("player", _stagehandNotifications.WrapEntityName(owned)),
                ("secretIdentity", Loc.GetString(PrototypeManager.Index(secretIdentityId).Name)));
            _stagehandNotifications.SendStagehandNotification(msg, ESStagehandNotificationSeverity.High);
        }
    }
}

/// <summary>
/// Raised on a organization entity and broadcast when an entity's secret identity changes.
/// </summary>
/// <remarks>
/// This is raised both when an identity is removed, and when a new one is applied.
/// So, it will be raised twice if something fully changes an entity's secret identity (e.g. conversion)
/// When being removed, <see cref="NewSecretIdentity"/> will be null, and <see cref="OldSecretIdentity"/> will have the old identity,
/// and vice versa for being applied.
/// </remarks>
[ByRefEvent]
public record struct ESSecretIdentityChangedEvent(Entity<MindComponent> Mind, ESSecretIdentityPrototype? NewSecretIdentity, ESSecretIdentityPrototype? OldSecretIdentity);

/// <summary>
///     Fired when players are being assigned to a organization. Old random assignment algorithm kicks in
///     if not handled. (This is a mild hack.)
/// </summary>
[ByRefEvent]
public record struct AssignPlayersToOrganizationEvent(bool Handled, List<ICommonSession> Players);

/// <summary>
///     Fired when players are latejoining. Old random assignment algorithm kicks in
///     if not handled. (This is a mild hack.)
/// </summary>
[ByRefEvent]
public record struct AssignLatejoinerToOrganizationEvent(bool Handled, ICommonSession Victim);
