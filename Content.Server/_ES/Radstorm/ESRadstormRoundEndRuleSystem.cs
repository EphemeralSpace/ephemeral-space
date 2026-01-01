using Content.Server._ES.Radio;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Shared._ES.CCVar;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._ES.Radstorm;

/// <summary>
///     Controls the radstorm round end behavior: after a certain amount of time, a radstorm will come and slowly kill everyone onboard the station.
///     This is announced on the shuttle, as well as announced
/// </summary>
public sealed class ESRadstormRoundEndRuleSystem : GameRuleSystem<ESRadstormRoundEndRuleComponent>
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override void Started(EntityUid uid,
        ESRadstormRoundEndRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        // don't override if it was set for whatever reason
        if (component.RadstormStartTime != TimeSpan.Zero)
            return;

        var randomMins = _random.NextGaussian(component.RadstormStartTimeAvg.TotalMinutes, component.RadstormStartTimeStdDev.TotalMinutes);

        // account for arrivals time
        if (_cfg.GetCVar(ESCVars.ESArrivalsEnabled))
            randomMins += (_cfg.GetCVar(ESCVars.ESArrivalsFTLTime) / 60f);

        // round to nearest minute
        randomMins = Math.Round(randomMins);

        component.RadstormStartTime = _timing.CurTime + TimeSpan.FromMinutes(randomMins);
        Log.Info($"Picked {randomMins} minutes into the round as the start time for the radstorm.");
    }

    protected override void ActiveTick(EntityUid uid, ESRadstormRoundEndRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        // can this even happen? idr (this is mostly so it doesnt try to end round twice)
        if (_ticker.RunLevel != GameRunLevel.InRound)
            return;

        var mapUid = _map.GetMap(_ticker.DefaultMap);

        if ((_timing.CurTime >= component.RadstormStartTime || component.SpaceDangerous)
            && _timing.CurTime >= component.RadstormNextDamageTickTime)
        {
            component.RadstormNextDamageTickTime = _timing.CurTime + TimeSpan.FromSeconds(1);

            // HELL EVERLASTING! DIE FOREVER!
            var stillAlive = 0;
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
                if (xform.ParentUid != mapUid && _timing.CurTime < component.RadstormStartTime)
                    continue;

                // only count mobs which actually end up taking damage from this
                var dmg = _damage.ChangeDamage((mob, damageable), component.RadstormDamagePerSecond, true, false);
                if (dmg.GetTotal() > FixedPoint2.Zero && state.CurrentState != MobState.Dead)
                    stillAlive += 1;
            }

            // show is over
            // (make sure we only actually do this if after time and not just deadly space)
            // (i kind of implemented that in a weird way huh)
            if (stillAlive == 0 && _timing.CurTime >= component.RadstormStartTime)
                _roundEnd.EndRound();

            return;
        }

        foreach (var phase in component.RadstormPhases)
        {
            if (phase.Completed)
                continue;

            var phaseStart = TimeSpan.Zero;
            if (phase.TimeBeforeEnd != null)
                phaseStart = component.RadstormStartTime - phase.TimeBeforeEnd.Value;
            else if (phase.TimeAfterStart != null)
                phaseStart = _ticker.RoundStartTimeSpan + phase.TimeAfterStart.Value;

            if (_timing.CurTime < phaseStart)
                continue;

            DoPhase(component, phase);
        }
    }

    private void DoPhase(ESRadstormRoundEndRuleComponent comp, ESRadstormPhaseConfig phase)
    {
        if (phase.AnnouncementText != null)
        {
            var minutes = (int) Math.Round((comp.RadstormStartTime - _ticker.RoundStartTimeSpan).TotalMinutes);
            var msg = Loc.GetString(phase.AnnouncementText, ("minutes", (minutes)));
            if (phase.AnnouncementDistortion > 0f)
                msg = FormattedMessage.RemoveMarkupPermissive(ESRadioSystem.DistortRadioMessage(msg, phase.AnnouncementDistortion, _proto, _random, Loc));
            _chat.DispatchGlobalAnnouncement(
                msg,
                Loc.GetString("es-radstorm-announcer"),
                announcementSound: phase.AnnouncementSound,
                colorOverride: Color.LightSeaGreen);
        }

        var map = _map.GetMap(_ticker.DefaultMap);
        if (phase.MapLight != null && TryComp<MapLightComponent>(map, out var mapLight))
        {
            mapLight.AmbientLightColor = phase.MapLight.Value;
            Dirty(map, mapLight);
        }

        if (phase.SpaceDangerous)
            comp.SpaceDangerous = true;

        phase.Completed = true;
    }
}
