using Content.Server._ES.Announcements;
using Content.Server._ES.Objectives;
using Content.Server._ES.WarpDrive.Components;
using Content.Server.Administration;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Shared._DV.Screens;
using Content.Shared._ES.Cinematic;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Nuke.Components;
using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Telesci.Components;
using Content.Shared._ES.WarpDrive;
using Content.Shared.Administration;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Content.Shared.Hands;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Toolshed;

namespace Content.Server._ES.WarpDrive;

/// <summary>
///     Handles all warp drive behavior
/// </summary>
/// <see cref="ESWarpDriveGameRuleComponent"/>
public sealed partial class ESWarpDriveSystem : GameRuleSystem<ESWarpDriveGameRuleComponent>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ESAnnouncementSystem _announcement = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private EntityTableSystem _table = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ESObjectiveSystem _objective = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private DeviceNetworkSystem _devicenet = default!;
    [Dependency] private ESCinematicSystem _cinematic = default!;
    [Dependency] private ESEntityTimerSystem _timer = default!;

    private static readonly TimeSpan EndRoundDuration = TimeSpan.FromSeconds(10);
    private static readonly ProtoId<ESCinematicPrototype> Cinematic = "WarpCinematic";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESWarpDriveObjectiveComponent, ESGetObjectiveProgressEvent>(OnGetObjectiveProgress);
        SubscribeLocalEvent<ESSingularityWorldInterruptionComponent, GotEquippedHandEvent>(OnInterruptionPickedUp);
        SubscribeLocalEvent<ESCryptoNukeSecurityOverridenEvent>(OnSecurityOverriden);

        Subs.BuiEvents<ESPortalGeneratorConsoleComponent>(ESPortalGeneratorConsoleUiKey.Key,
            subs =>
            {
                subs.Event<ESActivePortalGeneratorBuiMessage>(OnActivateWarpDrive);
            }
        );

        InitializeSingularityWorld();
    }

    private void OnSecurityOverriden(ref ESCryptoNukeSecurityOverridenEvent ev)
    {
        var query = EntityQueryEnumerator<ESWarpDriveGameRuleComponent>();
        while (query.MoveNext(out _, out var warp))
        {
            // How Did We Get Here?
            if (GetChargePercentage(warp) < 1f || warp.AllTerminalsOverridden)
                continue;

            if (warp.FirstTerminalOverriddenAt is null)
            {
                warp.FirstTerminalOverriddenAt = _timing.CurTime;
                warp.TerminalsOverridden = 0;
            }

            warp.TerminalsOverridden += 1;

            // blub hardblubcode
            if (warp.TerminalsOverridden >= 3)
            {
                warp.AllTerminalsOverridden = true;
                _announcement.DispatchRoundAnnouncement(Loc.GetString("es-warp-drive-announcement-can-activate"),
                    Loc.GetString("es-warpdrive-announcer"),
                    announcementSound: new SoundPathSpecifier("/Audio/_ES/Announcements/attention_high.ogg"),
                    colorOverride: Color.MediumVioletRed,
                    important: true);
            }
            else
            {
                var secondsLeft = (int) Math.Round(((warp.FirstTerminalOverriddenAt + warp.TerminalOverrideTime) - _timing.CurTime).Value.TotalSeconds);
                _announcement.DispatchRoundAnnouncement(Loc.GetString($"es-warp-drive-security-override-announcement-{warp.TerminalsOverridden}", ("seconds", secondsLeft)),
                    Loc.GetString("es-security-override-announcer"),
                    announcementSound: new SoundPathSpecifier("/Audio/_ES/Announcements/attention_medium.ogg"),
                    colorOverride: Color.LightGoldenrodYellow,
                    important: true);
            }
        }
    }

    private void OnActivateWarpDrive(EntityUid uid, ESPortalGeneratorConsoleComponent component, ESActivePortalGeneratorBuiMessage args)
    {
        var query = EntityQueryEnumerator<ESWarpDriveGameRuleComponent>();
        while (query.MoveNext(out var warpUid, out var warp))
        {
            if (!warp.AllTerminalsOverridden)
                continue;

            if (warp.InFinalPhase)
                continue;

            warp.FinalPhaseAt = _timing.CurTime;
            warp.InFinalPhase = true;
            UpdateAppearance(true);
            UpdateScreens((warpUid, warp), 1.0f);

            _announcement.DispatchRoundAnnouncement(Loc.GetString("es-warp-drive-announcement-final-phase-started"),
                Loc.GetString("es-warpdrive-announcer"),
                announcementSound: new SoundPathSpecifier("/Audio/_ES/Announcements/attention_high.ogg"),
                colorOverride: Color.MediumVioletRed,
                important: true);
        }
    }

    private void OnInterruptionPickedUp(Entity<ESSingularityWorldInterruptionComponent> ent, ref GotEquippedHandEvent args)
    {
        var query = EntityQueryEnumerator<ESWarpDriveGameRuleComponent>();
        while (query.MoveNext(out _, out var warp))
        {
            warp.LastClearer = args.User;
        }

        RemCompDeferred<ESSingularityWorldInterruptionComponent>(ent.Owner);
        _popup.PopupEntity(Loc.GetString("es-warp-drive-interruption-picked-up-user"), args.User, args.User);
    }

    private void OnGetObjectiveProgress(Entity<ESWarpDriveObjectiveComponent> ent, ref ESGetObjectiveProgressEvent args)
    {
        var query = EntityQueryEnumerator<ESWarpDriveGameRuleComponent>();
        while (query.MoveNext(out _, out var warp))
        {
            args.Progress = WarpDriveSuccess(warp) ? 1f : 0f;
        }
    }

    public float GetChargePercentage(ESWarpDriveGameRuleComponent component)
    {
        var totalTime = _timing.CurTime - _ticker.RoundStartTimeSpan - component.AccumulatedInterruptionTime;
        if (component.Interrupted && component.LastInterruptionTime is { } lastInterruption)
            totalTime -= _timing.CurTime - lastInterruption;
        return Math.Clamp((float) (totalTime / component.BaseChargeTime), 0f, 1f);
    }

    public float? GetFinalPhasePercentage(ESWarpDriveGameRuleComponent component)
    {
        if (!component.InFinalPhase || component.FinalPhaseAt is not { } startTime)
            return null;

        var elapsed = _timing.CurTime - startTime;
        return Math.Clamp((float)(elapsed / component.FinalPhaseTime), 0f, 1f);
    }

    public bool WarpDriveSuccess(ESWarpDriveGameRuleComponent component)
    {
        return component.InFinalPhase
               && component.FinalPhaseAt is { } startTime
               && _timing.CurTime > (startTime + component.FinalPhaseTime);
    }

    public bool CanInterrupt(ESWarpDriveGameRuleComponent component)
    {
        return !component.InFinalPhase && GetChargePercentage(component) < 1f;
    }

    protected override void Started(EntityUid uid,
        ESWarpDriveGameRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        component.NextInterruptionTime = _timing.CurTime + _random.Next(component.MinRandomInterruptionTime, component.MaxRandomInterruptionTime);

        StartedSingularityWorld(component);
    }

    protected override void ActiveTick(EntityUid uid, ESWarpDriveGameRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        ActiveTickSingularityWorld();

        // check if we should win from final phase ending
        if (WarpDriveSuccess(component) && !component.CinematicPlayed)
        {
            _objective.RefreshObjectiveProgress<ESWarpDriveObjectiveComponent>();

            var filter = Filter.Broadcast();
            var cinematic = ProtoMan.Index(Cinematic);
            _cinematic.PlayCinematic(Cinematic, filter);
            _timer.SpawnMethodTimer(cinematic.Length - EndRoundDuration,
                () =>
                {
                    _roundEnd.EndRound(EndRoundDuration);
                });

            component.CinematicPlayed = true;
            return;
        }

        var currentCharge = GetChargePercentage(component);

        if ((int)(currentCharge * 100) >= (component.LastScreenUpdatedChargePercentage + 5))
        {
            component.LastScreenUpdatedChargePercentage += 5;
            UpdateScreens((uid, component), currentCharge);
        }

        UpdateUiState(currentCharge, component.Interrupted, component.InFinalPhase, component.AllTerminalsOverridden);

        // check if we should play our announcements
        foreach (var announcement in component.Announcements)
        {
            if (announcement.Completed)
                continue;

            if (announcement.AfterChargePercentage is { } after && currentCharge < after)
                continue;

            if (announcement.AfterFinalPhasePercentage is { } afterFinal
                && (GetFinalPhasePercentage(component) is not { } percentage
                || percentage < afterFinal))
            {
                continue;
            }

            _announcement.DispatchRoundAnnouncement(Loc.GetString(announcement.Text),
                Loc.GetString("es-warpdrive-announcer"),
                announcementSound: announcement.Sound,
                colorOverride: Color.MediumVioletRed,
                important: true);

            announcement.Completed = true;
        }

        // terminal overriding and see if we need to reset them
        if (!component.AllTerminalsOverridden && component.FirstTerminalOverriddenAt is { } overrideTime)
        {
            if (_timing.CurTime > (overrideTime + component.TerminalOverrideTime))
            {
                // reset
                component.FirstTerminalOverriddenAt = null;
                component.TerminalsOverridden = 0;

                var terminalQuery = EntityQueryEnumerator<ESCryptoNukeConsoleComponent>();
                while (terminalQuery.MoveNext(out var consoleUid, out var console))
                {
                    console.WarpDriveSecurityOverridden = false;
                    Dirty(consoleUid, console);
                }

                _announcement.DispatchRoundAnnouncement(Loc.GetString("es-warp-drive-security-override-announcement-fail"),
                    Loc.GetString("es-security-override-announcer"),
                    announcementSound: new SoundPathSpecifier("/Audio/_ES/Announcements/attention_high.ogg"),
                    colorOverride: Color.LightGoldenrodYellow,
                    important: true);
            }
        }

        // early out of interruption logic if we're in final phase or charged
        if (!CanInterrupt(component))
            return;

        // check if we should make a new random interruption
        if (_timing.CurTime > component.NextInterruptionTime)
        {
            if (!component.Interrupted)
            {
                SpawnInterruptionObjects(component);
            }
        }

        // check if there are any active interrupting entities
        var interruptions = 0;
        var query = EntityQueryEnumerator<ESSingularityWorldInterruptionComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            if (xform.MapID != SingularityWorldMapId)
                continue;

            interruptions++;
        }

        if (interruptions <= 0 && component.Interrupted && component.LastInterruptionTime is { } time)
        {

            if (component.LastClearer is { } clearer)
            {
                var clearEv = new WarpDriveInterruptionClearedEvent(clearer);
                RaiseLocalEvent(clearer, ref clearEv);
            }

            component.LastClearer = null;

            component.Interrupted = false;
            component.AccumulatedInterruptionTime += _timing.CurTime - time;
            UpdateAppearance(true);
            UpdateScreens((uid, component), currentCharge);

            component.NextInterruptionTime = _timing.CurTime + _random.Next(component.MinRandomInterruptionTime, component.MaxRandomInterruptionTime);

            _announcement.DispatchRoundAnnouncement(Loc.GetString("es-warp-drive-announcement-interruptions-cleared"),
                Loc.GetString("es-warpdrive-announcer"),
                announcementSound: new SoundPathSpecifier("/Audio/_ES/Announcements/attention_low.ogg"),
                colorOverride: Color.MediumVioletRed);
        }
        else if (interruptions > 0 && !component.Interrupted)
        {
            component.Interrupted = true;
            component.LastInterruptionTime = _timing.CurTime;
            UpdateAppearance(false);
            UpdateScreens((uid, component), currentCharge);

            _announcement.DispatchRoundAnnouncement(Loc.GetString("es-warp-drive-announcement-interruptions-detected"),
                Loc.GetString("es-warpdrive-announcer"),
                announcementSound: new SoundPathSpecifier("/Audio/_ES/Announcements/attention_medium.ogg"),
                colorOverride: Color.MediumVioletRed);
        }
    }

    private void UpdateUiState(float charge, bool interrupted, bool finalPhase, bool allTerminals)
    {
        var query = EntityQueryEnumerator<ESPortalGeneratorConsoleComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            var state = new ESPortalGeneratorConsoleBuiState
            {
                Charge = charge,
                Interrupted = interrupted,
                FinalPhase = finalPhase,
                AllTerminals = allTerminals,
            };
            _ui.SetUiState(uid, ESPortalGeneratorConsoleUiKey.Key, state);
        }
    }

    private void UpdateAppearance(bool charging)
    {
        var query = EntityQueryEnumerator<ESWarpDriveComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _appearance.SetData(uid, ESWarpDriveVisuals.Charging, charging);
        }
    }

    private void UpdateScreens(Entity<ESWarpDriveGameRuleComponent> ent, float charge)
    {
        if (!TryComp<DeviceNetworkComponent>(ent, out var netComp))
            return;

        var prefix = ent.Comp.Interrupted ? "es-warp-drive-screen-interrupted" : "es-warp-drive-screen-charging";
        if (charge >= 1f)
            prefix = "es-warp-drive-screen-charged";

        var text = (Loc.GetString($"{prefix}-line1"), Loc.GetString($"{prefix}-line2", ("charge", (int) (charge * 100))));
        var payload = new NetworkPayload
        {
            [DVScreenPackets.Text] = text,
            [DVScreenPackets.Content] = DVScreenContent.Text,
        };

        _devicenet.QueuePacket(ent, null, payload, netComp.TransmitFrequency, device: netComp);
    }

    private void IncrementTeleportedEntitiesCount()
    {
        var query = EntityQueryEnumerator<ESWarpDriveGameRuleComponent>();
        while (query.MoveNext(out _, out var warpDrive))
        {
            if (!CanInterrupt(warpDrive))
                continue;

            warpDrive.ItemsTeleportedSinceLastInterruption += 1;
            if (warpDrive.ItemsTeleportedSinceLastInterruption > warpDrive.ManualInterruptionItems
                && warpDrive is { Interrupted: false, InFinalPhase: false })
            {
                warpDrive.ItemsTeleportedSinceLastInterruption = 0;
                SpawnInterruptionObjects(warpDrive);
            }
        }
    }

    public void SpawnInterruptionObjects(ESWarpDriveGameRuleComponent component)
    {
        if (SingularityWorldGrids is null || _proto.Index(component.InterruptionTrashTable) is not  { } table)
            return;

        // spawn a bunch of bull shit
        var amt = _random.Next(component.MinInterruptionTrashSpawns, component.MaxInterruptionTrashSpawns);
        while (amt > 0)
        {
            if (_spawnRegion.TryGetRandomCoordsInRegion(TeleportInWorld, SingularityWorldGrids, out var coords))
            {
                foreach (var entry in _table.GetSpawns(table))
                {
                    var ent = SpawnAtPosition(entry, coords.Value);
                    EnsureComp<ESSingularityWorldInterruptionComponent>(ent);
                }
            }
            amt--;
        }

        // no announcement thats handled later by it noticing
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class CauseWarpDriveInterruptionCommand : ToolshedCommand
{
    private ESWarpDriveSystem? _sys;

    [CommandImplementation]
    public void CauseWarpDriveInterruption()
    {
        _sys ??= GetSys<ESWarpDriveSystem>();
        var query = EntityManager.EntityQueryEnumerator<ESWarpDriveGameRuleComponent>();
        while (query.MoveNext(out _, out var rule))
        {
            _sys.SpawnInterruptionObjects(rule);
        }
    }
}

/// <summary>
///     raised when the warp drive is cleared
/// </summary>
[ByRefEvent]
public record struct WarpDriveInterruptionClearedEvent(EntityUid Entity);

