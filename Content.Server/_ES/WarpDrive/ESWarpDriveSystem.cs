using Content.Server._ES.WarpDrive.Components;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Content.Shared.Hands;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.WarpDrive;

/// <summary>
///     Handles all warp drive behavior
/// </summary>
/// <see cref="ESWarpDriveGameRuleComponent"/>
public sealed partial class ESWarpDriveSystem : GameRuleSystem<ESWarpDriveGameRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly EntityTableSystem _table = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESSingularityWorldInterruptionComponent, GotEquippedHandEvent>(OnInterruptionPickedUp);

        InitializeSingularityWorld();
    }

    private void OnInterruptionPickedUp(Entity<ESSingularityWorldInterruptionComponent> ent, ref GotEquippedHandEvent args)
    {
        RemCompDeferred<ESSingularityWorldInterruptionComponent>(ent.Owner);
        _popup.PopupEntity(Loc.GetString("es-warp-drive-interruption-picked-up-user"), args.User, args.User);
    }

    public float GetChargePercentage(ESWarpDriveGameRuleComponent component)
    {
        var totalTime = (_timing.CurTime - _ticker.RoundStartTimeSpan) - component.AccumulatedInterruptionTime;
        return (float) (totalTime / component.BaseChargeTime);
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

        ActiveTickSingularityWorld(component, frameTime);

        // check if there are any active interrupting entities
        var interruptions = 0;
        var query = EntityQueryEnumerator<ESSingularityWorldInterruptionComponent, TransformComponent>();
        while (query.MoveNext(out var interruption, out _, out var xform))
        {
            if (xform.MapID != SingularityWorldMapId)
                continue;

            interruptions++;
        }

        if (interruptions == 0 && component.Interrupted && component.LastInterruptionTime is { } time)
        {
            component.Interrupted = false;
            component.AccumulatedInterruptionTime += (_timing.CurTime - time);

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

            _chat.DispatchGlobalAnnouncement(
                Loc.GetString("es-warp-drive-announcement-interruptions-detected"),
                Loc.GetString("es-warpdrive-announcer"),
                announcementSound: new SoundPathSpecifier("/Audio/_ES/Announcements/attention_medium.ogg"),
                colorOverride: Color.MediumVioletRed);
        }

        // check if we should make a new random interruption
        if (_timing.CurTime > component.NextInterruptionTime)
        {
            // uhh... i seem to have.. caught you at a very interrupted time..
            // lets try again in a bit
            if (component.Interrupted)
            {
                component.NextInterruptionTime = _timing.CurTime + _random.Next(
                    component.MinRandomInterruptionTime / 2,
                    component.MaxRandomInterruptionTime / 2);
            }
            else
            {

            }
        }
    }

    private void IncrementTeleportedEntitiesCount(EntityUid teleportedEntity)
    {
        var query = EntityQueryEnumerator<ESWarpDriveGameRuleComponent>();
        while (query.MoveNext(out _, out var warpDrive))
        {
            warpDrive.ItemsTeleportedSinceLastInterruption += 1;
            if (warpDrive.ItemsTeleportedSinceLastInterruption > warpDrive.ManualInterruptionItems
                && warpDrive is { Interrupted: false, InFinalPhase: false })
            {
                warpDrive.ItemsTeleportedSinceLastInterruption = 0;
            }
            else if (warpDrive.ItemsTeleportedSinceLastInterruption > warpDrive.FinalPhaseForceEndItems
                     && warpDrive.InFinalPhase)
            {
                warpDrive.InFinalPhase = false;
            }
        }
    }

    private void CauseInterruption(ESWarpDriveGameRuleComponent component)
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
