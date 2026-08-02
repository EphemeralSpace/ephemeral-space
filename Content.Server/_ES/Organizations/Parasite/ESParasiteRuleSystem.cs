using Content.Server._ES.SecretIdentity;
using Content.Server._ES.Objectives;
using Content.Server._ES.Organizations.Parasite.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared._ES.Cinematic;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Core.Timer.Components;
using Content.Shared._ES.Objectives.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Systems;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.Audio;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Organizations.Parasite;

public sealed partial class ESParasiteRuleSystem : EntitySystem
{
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ESCinematicSystem _cinematic = default!;
    [Dependency] private ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private ESSecretIdentitySystem _secretIdentity = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private ESObjectiveSystem _objective = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private RejuvenateSystem _rejuvenate = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private StationSpawningSystem _stationSpawning = default!;

    /// <summary>
    ///     Round will actually end (screen pops up music plays etc) this amount of time before the cinematic finishes.
    /// </summary>
    private static readonly TimeSpan EndRoundDuration = TimeSpan.FromSeconds(10);
    private static readonly ProtoId<ESCinematicPrototype> Cinematic = "ParasiteCinematic";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESObjectiveProgressChangedEvent>(OnProgressChanged);

        SubscribeLocalEvent<ESParasiteRuleComponent, ESParasiteSwarmTimerEvent>(OnSwarmTimer);
        SubscribeLocalEvent<ESParasiteRuleComponent, ESParasiteWinCheckTimerEvent>(OnWinCheckTimer);
        SubscribeLocalEvent<ESParasiteConverterComponent, MeleeHitEvent>(OnHit);
    }

    private void OnProgressChanged(ref ESObjectiveProgressChangedEvent args)
    {
        var query = EntityQueryEnumerator<ESParasiteRuleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_objective.HasObjective(uid, args.Objective))
                continue;

            if (!_gameTicker.IsGameRuleActive(uid))
                continue;

            if (comp.ObjectivesCompleted)
                continue;

            if (!_objective.AllCompleted(uid))
                continue;

            StartEndPhase((uid, comp));
        }
    }

    private void OnSwarmTimer(Entity<ESParasiteRuleComponent> ent, ref ESParasiteSwarmTimerEvent args)
    {
        ent.Comp.SwarmStarted = true;
        TransformOrganizationMembers(ent);
    }

    private void OnWinCheckTimer(Entity<ESParasiteRuleComponent> ent, ref ESParasiteWinCheckTimerEvent args)
    {
        EndRound(ent);
    }

    private void EndRound(Entity<ESParasiteRuleComponent> ent)
    {
        if (ent.Comp.WinStarted)
            return;
        ent.Comp.WinStarted = true;

        var filter = Filter.Broadcast();
        var cinematic = ProtoMan.Index(Cinematic);
        _cinematic.PlayCinematic(Cinematic, filter);
        _entityTimer.SpawnMethodTimer(cinematic.Length - EndRoundDuration,
            () =>
            {
                _roundEnd.EndRound(EndRoundDuration);
            });

        // pause station map
        _map.SetPaused(_gameTicker.DefaultMap, true);
    }

    private void OnHit(Entity<ESParasiteConverterComponent> ent, ref MeleeHitEvent args)
    {
        foreach (var hit in args.HitEntities)
        {
            if (!_mind.TryGetMind(hit, out var mind))
                continue;

            if (_mind.IsCharacterDeadIc(mind))
                continue;

            if (_secretIdentity.GetOrganizationOrNull(mind.Value.AsNullable()) == ent.Comp.IgnoreOrganization)
                continue;

            if (_actionBlocker.CanMove(hit))
                continue;

            _secretIdentity.ChangeSecretIdentity(mind.Value, ent.Comp.SecretIdentity);
            _audio.PlayPvs(ent.Comp.Sound, hit);
        }
    }

    private void StartEndPhase(Entity<ESParasiteRuleComponent> ent)
    {
        ent.Comp.ObjectivesCompleted = true;
        _objective.FreezeObjectives<ESParasiteWinFreezeObjectiveComponent>();

        var msg = Loc.GetString("es-parasite-swarm-notif");
        var wrappedMsg = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        foreach (var mind in _secretIdentity.GetOrganizationMembers(ent.Owner))
        {
            if (!TryComp<MindComponent>(mind, out var mindComp) ||
                !_playerManager.TryGetSessionById(mindComp.UserId, out var session))
                continue;

            _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMsg, default, false, session.Channel, Color.YellowGreen);
        }

        _entityTimer.SpawnTimer(ent, ent.Comp.SwarmDelay, new ESParasiteSwarmTimerEvent());
        _entityTimer.SpawnTimer(ent, ent.Comp.SwarmDelay + ent.Comp.WinDelay, new ESParasiteWinCheckTimerEvent());
    }

    private void TransformOrganizationMembers(Entity<ESParasiteRuleComponent> ent)
    {
        foreach (var mind in _secretIdentity.GetOrganizationMembers(ent.Owner))
        {
            if (!TryComp<MindComponent>(mind, out var mindComp ) ||
                mindComp.OwnedEntity is not { } owned)
                continue;

            _popup.PopupEntity(Loc.GetString("es-parasite-burst-popup", ("ent", Identity.Entity(owned, EntityManager))), owned, PopupType.LargeCaution);
            _audio.PlayPvs(ent.Comp.BurstSound, owned);

            _rejuvenate.PerformRejuvenate(ent);
            _stationSpawning.EquipStartingGear(owned, ent.Comp.SwarmGear);
        }
    }

    private bool AllPlayersConverted(EntityUid organization)
    {
        var nonOrganizationCount = 0;
        foreach (var mind in _secretIdentity.GetNotOrganizationMembers(organization))
        {
            if (!TryComp<MindComponent>(mind, out var mindComp))
                continue;

            if (!_mind.IsCharacterDeadIc(mindComp))
                ++nonOrganizationCount;
        }

        return nonOrganizationCount == 0;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ESParasiteRuleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.SwarmStarted)
                continue;

            if (!AllPlayersConverted(uid))
                continue;
            EndRound((uid, comp));
        }
    }
}

public sealed partial class ESParasiteSwarmTimerEvent : ESEntityTimerEvent;

public sealed partial class ESParasiteWinCheckTimerEvent : ESEntityTimerEvent;
