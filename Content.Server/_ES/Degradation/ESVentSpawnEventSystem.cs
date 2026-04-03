using Content.Server._ES.Degradation.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Popups;
using Content.Server.StationEvents.Events;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Core.Timer.Components;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Degradation;

public sealed class ESVentSpawnEventSystem : StationEventSystem<ESVentSpawnEventComponent>
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESVentSpawnEventComponent, ESVentSpawnEntityTimerEvent>(OnVentSpawnEntityTimer);
    }

    private void OnVentSpawnEntityTimer(Entity<ESVentSpawnEventComponent> ent, ref ESVentSpawnEntityTimerEvent args)
    {
        if (!args.Coordinates.IsValid(EntityManager))
            return;

        var spawn = SpawnAtPosition(args.Entity, args.Coordinates);
        _popup.PopupEntity(Loc.GetString("es-vent-swarm-popup", ("spawn", spawn)), spawn);
    }

    protected override void Started(EntityUid uid, ESVentSpawnEventComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var ventList = new List<EntityCoordinates>();
        var query = EntityQueryEnumerator<GasVentPumpComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var xform))
        {
            ventList.Add(xform.Coordinates);
        }

        if (ventList.Count == 0)
            return;

        foreach (var prototype in _entityTable.GetSpawns(component.Table))
        {
            var delay = RobustRandom.Next(component.MinSpawnDelay, component.MaxSpawnDelay);
            var coords = RobustRandom.Pick(ventList);
            _entityTimer.SpawnTimer(uid, delay, new ESVentSpawnEntityTimerEvent(prototype, coords));
        }
    }
}

public sealed partial class ESVentSpawnEntityTimerEvent : ESEntityTimerEvent
{
    [DataField]
    public EntProtoId? Entity;

    [DataField]
    public EntityCoordinates Coordinates = EntityCoordinates.Invalid;

    public ESVentSpawnEntityTimerEvent(EntProtoId entity, EntityCoordinates coordinates)
    {
        Entity = entity;
        Coordinates = coordinates;
    }
}
