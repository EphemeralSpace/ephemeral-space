using Content.Server._ES.StationEvents.VentSwarm.Components;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._ES.Voting.Components;
using Content.Shared._ES.Voting.Results;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Utility;

namespace Content.Server._ES.StationEvents.VentSwarm;

public sealed class ESVentSwarmRule : StationEventSystem<ESVentSwarmRuleComponent>
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESVentSwarmRuleComponent, ESSynchronizedVotesCompletedEvent>(OnVotesCompleted);
    }

    private void OnVotesCompleted(Entity<ESVentSwarmRuleComponent> ent, ref ESSynchronizedVotesCompletedEvent args)
    {
        if (!args.TryGetResult<ESEntityVoteOption>(0, out var ventOption) ||
            !TryGetEntity(ventOption.Entity, out var vent))
        {
            ForceEndSelf(ent);
            return;
        }

        ent.Comp.Vent = vent.Value;

        if (TryComp<StationEventComponent>(ent, out var station))
        {
            station.StartAnnouncement = Loc.GetString("es-station-event-vent-swarm-start-announcement",
                ("location", FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(ent.Comp.Vent.Value))));
        }
    }

    protected override void Started(EntityUid uid, ESVentSwarmRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.NextSwarmTime = Timing.CurTime;
        component.SwarmCount = RobustRandom.Next(component.MinSwarmCount, component.MaxSwarmCount + 1);
    }

    protected override void ActiveTick(EntityUid uid, ESVentSwarmRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (!component.Vent.HasValue || TerminatingOrDeleted(component.Vent))
        {
            ForceEndSelf(uid, gameRule);
            return;
        }

        if (Timing.CurTime < component.NextSwarmTime)
            return;
        component.NextSwarmTime += RobustRandom.Next(component.MinSwarmDelay, component.MaxSwarmDelay);

        foreach (var spawn in _entityTable.GetSpawns(component.SpawnTable))
        {
            var ent = SpawnNextToOrDrop(spawn, component.Vent.Value);
            _popup.PopupEntity(Loc.GetString("es-vent-swarm-popup", ("spawn", ent)), ent);
        }

        component.SwarmCount--;
        if (component.SwarmCount <= 0)
            ForceEndSelf(uid, gameRule);
    }
}
