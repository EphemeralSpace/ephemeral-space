using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Singularity.Events;
using Content.Server.Spreader;
using Content.Shared._ES.Power.Antimatter;
using Content.Shared._ES.Power.Antimatter.Components;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Item;
using Content.Shared.Repairable;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server._ES.Power.Antimatter;

public sealed class ESAntimatterSystem : ESSharedAntimatterSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IViewVariablesManager _viewVariables = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly MapSystem _map = default!;

    private EntityQuery<ItemComponent> _itemQuery;

    private readonly HashSet<Entity<ESAntimatterComponent>> _antimatterSet = new();
    private readonly HashSet<Entity<ESAntimatterConverterComponent>> _converterSet = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESAntimatterComponent, SpreadNeighborsEvent>(OnSpreadNeighbors);
        SubscribeLocalEvent<ESAntimatterComponent, AtmosDeviceUpdateEvent>(OnDeviceUpdate);
        SubscribeLocalEvent<ESAntimatterComponent, EntityConsumedByEventHorizonEvent>(OnEntityConsumed);

        SubscribeLocalEvent<ESAntimatterConverterComponent, RepairedEvent>(OnRepaired);

        var vvHandle = _viewVariables.GetTypeHandler<ESAntimatterComponent>();
        vvHandle.AddPath(nameof(ESAntimatterComponent.Mass), (_, comp) => comp.Mass, SetMass);

        _itemQuery = GetEntityQuery<ItemComponent>();
    }

    private void OnSpreadNeighbors(Entity<ESAntimatterComponent> ent, ref SpreadNeighborsEvent args)
    {
        var xform = Transform(ent);

        var filteredNeighborFreeTiles = new ValueList<(MapGridComponent Grid, TileRef Tile)>();
        foreach (var neighbor in args.NeighborFreeTiles)
        {
            _converterSet.Clear();
            _lookup.GetLocalEntitiesIntersecting(xform.GridUid!.Value, neighbor.Tile.GridIndices, _converterSet);
            if (_converterSet.Count > 0)
                continue;
            filteredNeighborFreeTiles.Add(neighbor);
        }

        if (filteredNeighborFreeTiles.Count > 0 && args.Updates > 0)
        {
            _random.Shuffle(filteredNeighborFreeTiles);
            var transferAmount = (ent.Comp.Mass - ent.Comp.OverflowMass) / Math.Min(filteredNeighborFreeTiles.Count, args.Updates);

            if (transferAmount < ent.Comp.MinSpreadMass)
                return;

            foreach (var neighbor in filteredNeighborFreeTiles)
            {
                var pos = _map.GridTileToLocal(neighbor.Tile.GridUid, neighbor.Grid, neighbor.Tile.GridIndices);
                var newAntimatter = Spawn(ent.Comp.AntimatterProto, pos);

                var newAntimatterComp = Comp<ESAntimatterComponent>(newAntimatter);
                SetMass((newAntimatter, newAntimatterComp), transferAmount);
                SetMass(ent, ent.Comp.Mass - transferAmount);

                if (args.Updates <= 0)
                    break;
            }

            return;
        }

        var orderedNeighbors = new ValueList<Entity<ESAntimatterComponent>>();
        foreach (var neighbor in args.Neighbors)
        {
            if (!TryComp<ESAntimatterComponent>(neighbor, out var neighborComp))
                continue;

            orderedNeighbors.Add((neighbor, neighborComp));
        }
        var totalVolume = orderedNeighbors.Sum(e => e.Comp.Mass);
        orderedNeighbors.Sort((e1, e2) => e1.Comp.Mass.CompareTo(e2.Comp.Mass));

        if (args.Neighbors.Count > 0)
        {
            foreach (var neighbor in orderedNeighbors)
            {
                if (neighbor.Comp.Mass > ent.Comp.Mass)
                    continue;

                var idealAverageVolume =
                    (totalVolume + (ent.Comp.Mass - ent.Comp.OverflowMass) + neighbor.Comp.OverflowMass) / (args.Neighbors.Count + 1);

                if (idealAverageVolume > ent.Comp.Mass)
                    continue;

                var transfer = idealAverageVolume - neighbor.Comp.Mass;
                if (transfer <= 0)
                    continue;

                SetMass(neighbor, neighbor.Comp.Mass + transfer);
                SetMass(ent, ent.Comp.Mass - transfer);
                args.Updates--;

                if (args.Updates <= 0)
                    break;
            }
        }

        if (filteredNeighborFreeTiles.Count == 0 &&
            args.Neighbors.Count < 4 &&
            ent.Comp.Mass > ent.Comp.ExplosionMassAmount)
        {
            _explosion.TriggerExplosive(ent);
        }
    }

    private void OnDeviceUpdate(Entity<ESAntimatterComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        if (_atmosphere.GetTileMixture(ent.Owner) is not { } mixture)
            return;

        var consumedVolume = mixture.RemoveVolume(Atmospherics.CellVolume * args.dt);
        SetMass(ent, ent.Comp.Mass + consumedVolume.GetMoles(ent.Comp.GrowthGas) * ent.Comp.GrowthPerGas);
    }

    private void OnEntityConsumed(Entity<ESAntimatterComponent> ent, ref EntityConsumedByEventHorizonEvent args)
    {
        // Hack because of dumb singularity code.
        if (HasComp<SolutionComponent>(args.Entity))
            return;

        var area = _itemQuery.TryComp(args.entity, out var item)
            ? _item.GetItemShape((args.entity, item)).GetArea()
            : 16; // arbitrary number

        var massAdjustment = MathF.Pow(area, 2) / 2f;
        SetMass(ent, ent.Comp.Mass - massAdjustment);

        _audio.PlayPvs(ent.Comp.ConsumeSound, Transform(ent).Coordinates);
    }

    private void OnRepaired(Entity<ESAntimatterConverterComponent> ent, ref RepairedEvent args)
    {
        ent.Comp.Broken = false;
        Dirty(ent);
        Appearance.SetData(ent, ESAntimatterConverterVisuals.Broken, false);
    }

    public void SetMass(EntityUid ent, float mass, ESAntimatterComponent? component = null)
    {
        if (!Resolve(ent, ref component))
            return;
        SetMass((ent, component), mass);
    }

    public override void SetMass(Entity<ESAntimatterComponent> ent, float mass)
    {
        ent.Comp.Mass = mass;

        if (mass <= 0)
        {
            QueueDel(ent);
            return;
        }

        if (ent.Comp.Mass > ent.Comp.OverflowMass)
        {
            EnsureComp<ActiveEdgeSpreaderComponent>(ent);
        }
        else
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(ent);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var converterQuery = EntityQueryEnumerator<ESAntimatterConverterComponent, BatteryComponent, TransformComponent>();
        while (converterQuery.MoveNext(out var uid, out var comp, out var battery, out var xform))
        {
            if (!xform.Anchored || comp.Broken)
                continue;

            if (Timing.CurTime < comp.NextUpdateTime)
                continue;
            comp.NextUpdateTime += TimeSpan.FromSeconds(1);

            _antimatterSet.Clear();
            _lookup.GetEntitiesInRange(xform.Coordinates, MathF.Sqrt(2), _antimatterSet);

            foreach (var antimatter in _antimatterSet)
            {
                var toRemove = Math.Min(antimatter.Comp.Mass, comp.RemovalAmount);
                SetMass(antimatter, antimatter.Comp.Mass - toRemove);
                _battery.SetCharge(uid, battery.CurrentCharge + toRemove * comp.EnergyPerMass, battery);
            }

            Appearance.SetData(uid, ESAntimatterConverterVisuals.Draining, _antimatterSet.Count != 0);
            PointLight.SetEnabled(uid, _antimatterSet.Count != 0);
        }
    }
}
