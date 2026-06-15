using Content.Server._ES.Objectives;
using Content.Server._ES.WarpDrive.Components;
using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Telesci.Components;
using Content.Shared._ES.WarpDrive;
using Content.Shared.Administration;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Content.Shared.Hands;
using Robust.Shared.Audio;
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
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private EntityTableSystem _table = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ESObjectiveSystem _objective = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESWarpDriveObjectiveComponent, ESGetObjectiveProgressEvent>(OnGetObjectiveProgress);
        SubscribeLocalEvent<ESSingularityWorldInterruptionComponent, GotEquippedHandEvent>(OnInterruptionPickedUp);

        Subs.BuiEvents<ESPortalGeneratorConsoleComponent>(ESPortalGeneratorConsoleUiKey.Key,
            subs =>
            {
                subs.Event<ESActivePortalGeneratorBuiMessage>(OnActivateWarpDrive);
            }
        );

        InitializeSingularityWorld();
    }

    private void OnActivateWarpDrive(EntityUid uid, ESPortalGeneratorConsoleComponent component, ESActivePortalGeneratorBuiMessage args)
    {
        var query = EntityQueryEnumerator<ESWarpDriveGameRuleComponent>();
        while (query.MoveNext(out _, out var warp))
        {
            if (warp.InFinalPhase)
                continue;

            warp.FinalPhaseAt = _timing.CurTime;
            warp.InFinalPhase = true;
            UpdateAppearance(true);

            _chat.DispatchGlobalAnnouncement(
                Loc.GetString("es-warp-drive-announcement-final-phase-started"),
                Loc.GetString("es-warpdrive-announcer"),
                announcementSound: new SoundPathSpecifier("/Audio/_ES/Announcements/attention_high.ogg"),
                colorOverride: Color.MediumVioletRed);
        }
    }

    private void OnInterruptionPickedUp(Entity<ESSingularityWorldInterruptionComponent> ent, ref GotEquippedHandEvent args)
    {
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
        return Math.Clamp((float) (totalTime / component.BaseChargeTime), 0, 1);
    }

    public bool WarpDriveSuccess(ESWarpDriveGameRuleComponent component)
    {
        return component.InFinalPhase
               && component.FinalPhaseAt is { } startTime
               && _timing.CurTime > (startTime + component.FinalPhaseTime);
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
        if (WarpDriveSuccess(component))
        {
            _objective.RefreshObjectiveProgress<ESWarpDriveObjectiveComponent>();
            _roundEnd.EndRound(TimeSpan.FromMinutes(1));
            return;
        }

        // check if we should play our announcements
        var currentCharge = GetChargePercentage(component);
        UpdateUiState(currentCharge, component.Interrupted, component.InFinalPhase);
        foreach (var announcement in component.Announcements)
        {
            if (announcement.Completed)
                continue;

            if (currentCharge < announcement.AfterChargePercentage)
                continue;

            _chat.DispatchGlobalAnnouncement(
                Loc.GetString(announcement.Text),
                Loc.GetString("es-warpdrive-announcer"),
                announcementSound: announcement.Sound,
                colorOverride: Color.MediumVioletRed);

            announcement.Completed = true;
        }

        // check if we should make a new random interruption
        if (!component.InFinalPhase && _timing.CurTime > component.NextInterruptionTime)
        {
            if (!component.Interrupted)
            {
                SpawnInterruptionObjects(component);
            }

            component.NextInterruptionTime = _timing.CurTime + _random.Next(component.MinRandomInterruptionTime, component.MaxRandomInterruptionTime);
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
            component.Interrupted = false;
            component.AccumulatedInterruptionTime += (_timing.CurTime - time);
            UpdateAppearance(true);

            _chat.DispatchGlobalAnnouncement(
                Loc.GetString("es-warp-drive-announcement-interruptions-cleared"),
                Loc.GetString("es-warpdrive-announcer"),
                announcementSound: new SoundPathSpecifier("/Audio/_ES/Announcements/attention_low.ogg"),
                colorOverride: Color.MediumVioletRed);
        }
        else if (interruptions > 0 && !component.Interrupted)
        {
            component.Interrupted = true;
            component.LastInterruptionTime = _timing.CurTime;
            UpdateAppearance(false);

            _chat.DispatchGlobalAnnouncement(
                Loc.GetString("es-warp-drive-announcement-interruptions-detected"),
                Loc.GetString("es-warpdrive-announcer"),
                announcementSound: new SoundPathSpecifier("/Audio/_ES/Announcements/attention_medium.ogg"),
                colorOverride: Color.MediumVioletRed);
        }
    }

    private void UpdateUiState(float charge, bool interrupted, bool finalPhase)
    {
        var query = EntityQueryEnumerator<ESPortalGeneratorConsoleComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            var state = new ESPortalGeneratorConsoleBuiState
            {
                Charge = charge,
                Interrupted = interrupted,
                FinalPhase = finalPhase,
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

    private void IncrementTeleportedEntitiesCount()
    {
        var query = EntityQueryEnumerator<ESWarpDriveGameRuleComponent>();
        while (query.MoveNext(out _, out var warpDrive))
        {
            warpDrive.ItemsTeleportedSinceLastInterruption += 1;
            if (warpDrive.ItemsTeleportedSinceLastInterruption > warpDrive.ManualInterruptionItems
                && warpDrive is { Interrupted: false, InFinalPhase: false })
            {
                warpDrive.ItemsTeleportedSinceLastInterruption = 0;
                SpawnInterruptionObjects(warpDrive);
            }
            else if (warpDrive.ItemsTeleportedSinceLastInterruption > warpDrive.FinalPhaseForceEndItems
                     && warpDrive.InFinalPhase)
            {
                warpDrive.ItemsTeleportedSinceLastInterruption = 0;
                warpDrive.InFinalPhase = false;
                _chat.DispatchGlobalAnnouncement(
                    Loc.GetString("es-warp-drive-announcement-final-phase-force-ended"),
                    Loc.GetString("es-warpdrive-announcer"),
                    announcementSound: new SoundPathSpecifier("/Audio/_ES/Announcements/attention_high.ogg"),
                    colorOverride: Color.MediumVioletRed);
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
