using System.Diagnostics.CodeAnalysis;
using Content.Server._ES.Announcements;
using Content.Server._ES.Radio;
using Content.Server._ES.Radstorm.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Shared._DV.Screens;
using Content.Shared._ES.CCVar;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._ES.Radstorm;

/// <summary>
///     Controls the radstorm round end behavior: after a certain amount of time, a radstorm will come and slowly kill everyone onboard the station.
///     This is announced on the shuttle, as well as announced
/// </summary>
public sealed partial class ESRadstormRoundEndRuleSystem : GameRuleSystem<ESRadstormRoundEndRuleComponent>
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private BrainDamageSystem _brainDamage = default!;
    [Dependency] private DeviceNetworkSystem _devicenet = default!;
    [Dependency] private ESAnnouncementSystem _chat = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedRoofSystem _roof = default!;

    protected override void Started(EntityUid uid,
        ESRadstormRoundEndRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        // don't override if it was set for whatever reason
        if (component.RadstormTimeRemaining != TimeSpan.Zero)
            return;

        var randomMins = _random.NextGaussian(component.RadstormStartTimeAvg.TotalMinutes, component.RadstormStartTimeStdDev.TotalMinutes);

        // account for arrivals time
        if (_cfg.GetCVar(ESCVars.ESArrivalsEnabled))
            randomMins += (_cfg.GetCVar(ESCVars.ESArrivalsFTLTime) / 60f);

        // round to nearest minute
        randomMins = Math.Round(randomMins);

        component.NextUpdateTime = _timing.CurTime + component.UpdateRate;
        component.RadstormTimeRemaining = TimeSpan.FromMinutes(randomMins);
        component.RadstormDuration = TimeSpan.FromMinutes(randomMins);
        Log.Info($"Picked {randomMins} minutes into the round as the start time for the radstorm.");

        UpdateScreenTimers((uid, component), component.RadstormTimeRemaining);
    }

    protected override void ActiveTick(EntityUid uid, ESRadstormRoundEndRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        // can this even happen? idr (this is mostly so it doesnt try to end round twice)
        if (_ticker.RunLevel != GameRunLevel.InRound)
            return;

        if (_timing.CurTime >= component.NextUpdateTime)
        {
            component.NextUpdateTime += component.UpdateRate;
            component.RadstormTimeRemaining -= component.UpdateRate * GetRadstormSpeedMultiplier();
        }

        var mapUid = _map.GetMap(_ticker.DefaultMap);

        if ((RadstormStarted((uid, component)) || component.SpaceDangerous)
            && _timing.CurTime >= component.RadstormNextDamageTickTime)
        {
            component.RadstormNextDamageTickTime = _timing.CurTime + TimeSpan.FromSeconds(1);

            // this should probably not be bounded to mobstate and instead be its own thing but whatever
            var killQuery = EntityQueryEnumerator<MobStateComponent, DamageableComponent, TransformComponent>();
            while (killQuery.MoveNext(out var mob, out var state, out var damageable, out var xform))
            {
                if (xform.MapID != _ticker.DefaultMap)
                    continue;

                if (state.CurrentState == MobState.Dead)
                    continue;

                // if they're not in space (i.e. not parented to the map)
                // and we haven't technically started yet, that means we're only space-dangerous, so don't hurt them
                if (xform.ParentUid != mapUid && !RadstormStarted((uid, component)))
                    continue;

                if (TryComp<BrainDamageComponent>(mob, out var brainDamage))
                    _brainDamage.TryChangeBrainDamage((mob, brainDamage), brainDamage.MaxDamage / 20);

                _damage.ChangeDamage((mob, damageable), component.RadstormDamagePerSecond, true, false);
            }
        }

        // If everyone's dead, end the round
        var actorQuery = EntityQueryEnumerator<ActorComponent>();
        var allDead = true;
        while (actorQuery.MoveNext(out var mob, out _))
        {
            if (TryComp<MobStateComponent>(mob, out var state) && state.CurrentState != MobState.Dead)
                allDead = false;
        }

        if (allDead)
        {
            _roundEnd.EndRound();
            return;
        }

        foreach (var phase in component.RadstormPhases)
        {
            if (!CanStartPhase((uid, component), phase))
                continue;

            DoPhase(component, phase);
        }
    }

    private void DoPhase(ESRadstormRoundEndRuleComponent comp, ESRadstormPhaseConfig phase)
    {
        if (phase.AnnouncementText != null)
        {
            var minutes = (int) Math.Round(GetRadstormEstimatedArrivalTime().TotalMinutes);
            var msg = Loc.GetString(phase.AnnouncementText, ("minutes", (minutes)));
            if (phase.AnnouncementDistortion > 0f)
                msg = FormattedMessage.RemoveMarkupPermissive(ESRadioSystem.DistortRadioMessage(msg, phase.AnnouncementDistortion, _proto, _random, Loc));

            _chat.DispatchRoundAnnouncement(msg,
                Loc.GetString("es-radstorm-announcer"),
                announcementSound: phase.AnnouncementSound,
                colorOverride: Color.LightSeaGreen,
                important: true);
        }

        // if text is null but sound isnt, this phase just wants to play a sound with no announcement
        if (phase.AnnouncementText == null && phase.AnnouncementSound != null)
        {
            _audio.PlayGlobal(phase.AnnouncementSound, Filter.Broadcast(), true, phase.AnnouncementSound.Params.WithVolume(-2f));
        }

        var map = _map.GetMap(_ticker.DefaultMap);
        if (phase.MapLight != null && TryComp<MapLightComponent>(map, out var mapLight))
        {
            mapLight.AmbientLightColor = phase.MapLight.Value;
            Dirty(map, mapLight);
        }

        // todo this is silly jank do it better like with postprocess etc
        if (phase.RemoveGridRoof)
        {
            foreach (var grid in _map.GetAllGrids(_ticker.DefaultMap))
            {
                // this is kinda inefficient but like idk
                var enumerator = _map.GetAllTilesEnumerator(grid.Owner, grid.Comp, ignoreEmpty: true);
                while (enumerator.MoveNext(out var tile))
                {
                    _roof.SetRoof((grid.Owner, grid.Comp), tile.Value.GridIndices, false);
                }
            }
        }

        if (phase.SpaceDangerous)
            comp.SpaceDangerous = true;

        phase.Completed = true;
    }

    private bool RadstormStarted(Entity<ESRadstormRoundEndRuleComponent> ent)
    {
        return ent.Comp.RadstormTimeRemaining <= TimeSpan.Zero;
    }

    private bool CanStartPhase(Entity<ESRadstormRoundEndRuleComponent> ent, ESRadstormPhaseConfig phase)
    {
        // Don't start a phase which has already completed.
        if (phase.Completed)
            return false;

        if (phase.TimeAfterStart.HasValue)
        {
            return ent.Comp.ElapsedRadstormTime >= phase.TimeAfterStart;
        }

        if (phase.TimeBeforeEnd.HasValue)
        {
            return ent.Comp.RadstormTimeRemaining <= phase.TimeBeforeEnd.Value;
        }

        throw new Exception("Phase has no valid start condition!");
    }

    public void UpdateScreenTimers(Entity<ESRadstormRoundEndRuleComponent> ent, TimeSpan newTime)
    {
        // Show timer on screen
        if (!TryComp<DeviceNetworkComponent>(ent, out var netComp))
            return;

        (string?, string?) text = (Loc.GetString("es-radstorm-screen-line-1"), null);
        var payload = new NetworkPayload
        {
            [DVScreenPackets.Text] = text,
            [DVScreenPackets.Content] = DVScreenContent.GenericTargetTime,
            [DVScreenPackets.Time] = newTime,
        };

        _devicenet.QueuePacket(ent, null, payload, netComp.TransmitFrequency, device: netComp);
    }

    private float GetRadstormSpeedMultiplier()
    {
        var ev = new GetRadstormSpeedMultiplierEvent();
        RaiseLocalEvent(ref ev);

        return ev.Speed;
    }

    private bool TryGetRadstormRule([NotNullWhen(true)] out Entity<ESRadstormRoundEndRuleComponent>? ent)
    {
        var query = EntityQueryEnumerator<ESRadstormRoundEndRuleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            ent = (uid, comp);
            return true;
        }

        ent = null;
        return false;
    }

    /// <summary>
    /// Gets the estimated time til the radstorm arrives, based on the current radstorm speed multiplier.
    /// </summary>
    public TimeSpan GetRadstormEstimatedArrivalTime()
    {
        if (!TryGetRadstormRule(out var ent))
            return TimeSpan.Zero;

        return ent.Value.Comp.RadstormTimeRemaining / GetRadstormSpeedMultiplier();
    }
}

/// <summary>
/// Event broadcast when determining the speed multiplier for the radstorm timer.
/// </summary>
[ByRefEvent]
public record struct GetRadstormSpeedMultiplierEvent()
{
    public float Speed = 1f;
}
