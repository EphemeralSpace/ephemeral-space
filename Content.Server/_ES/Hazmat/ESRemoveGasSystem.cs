using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos.Piping.Unary.Visuals;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Power;
using Content.Shared.Tools.Systems;
using JetBrains.Annotations;
using Content.Shared.Chemistry.Components;
using Content.Shared.Fluids.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Map.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Spawners;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.EntityEffects.Effects.Solution;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Decals;
using Content.Server.Decals;
using System.Numerics;

using Content.Shared._ES.Hazmat.Components;
using Content.Shared._ES.Hazmat;

namespace Content.Server._ES.Hazmat;

public sealed partial class ESRemoveGasSystem : ESSharedRemoveGasSystem
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private TransformSystem _transformSystem = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private DecalSystem _decalSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ESRemoveGasComponent, TimedDespawnEvent>(OnRemoveGasDoAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = Timing.CurTime;

        var query = EntityQueryEnumerator<ESRemoveGasComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextClean > curTime)
                continue;

            var transform = CompOrNull<TransformComponent>(uid);
            if (transform == null)
            {
                Log.Debug("RemoveGas! Grid or transform component not found.");
                continue; // unsure how to handle error
            }
            var environment = _atmosphereSystem.GetTileMixture((uid, transform), true);
            if (environment != null)
            {
                Scrub(frameTime, comp, environment);

                comp.NextClean += comp.UpdateInterval;
            }
        }
    }

    public void OnRemoveGasDoAfter(Entity<ESRemoveGasComponent> ent, ref TimedDespawnEvent args)
    {
        Log.Debug("RemoveGas! Triggering removal.");
        var uid = ent.Owner;
        if (!TryComp<TransformComponent>(uid, out var transform))
        {
            Log.Debug("RemoveGas! Transform not found.");
            return;
        }

        if (!TryComp<MapGridComponent>(transform.GridUid, out var mapGrid))
        {
            Log.Debug("RemoveGas! Map Grid not found");
            return;
        }

        if (!TryComp<SmokeComponent>(uid, out var smokeComponent))
            return;

        if (!_solutionContainerSystem.ResolveSolution(uid, SmokeComponent.SolutionName, ref smokeComponent.Solution, out var solution) || !solution.Any())
        {
            Log.Debug("RemoveGas! Solution to clean up not found.");
            return;
        }

        var tile = _map.GetTileRef(transform.GridUid.Value, mapGrid, transform.Coordinates);
        if (tile.Tile.IsEmpty)
        {
            Log.Debug("RemoveGas! No tile detected.");
            return;
        }

        var canDoDecals = TryComp<DecalGridComponent>(tile.GridUid, out var decalGrid);

        var lookupSystem = EntityManager.System<EntityLookupSystem>();
        var entities = lookupSystem.GetLocalEntitiesIntersecting(tile, 0f).ToArray();

        foreach (var reagentQuantity in solution.Contents.ToArray())
        {
            var reactVolume = reagentQuantity.Quantity;
            var reagent = _prototype.Index<ReagentPrototype>(reagentQuantity.Reagent.Prototype);
            var puddleQuery = GetEntityQuery<PuddleComponent>();

            var purgeAmount = reactVolume / 0.1f;

            foreach (var entity in entities)
            {
                if (!puddleQuery.TryGetComponent(entity, out var puddle) ||
                    !_solutionContainerSystem.TryGetSolution(entity, puddle.SolutionName, out var puddleSolution, out _))
                {
                    continue;
                }

                // todo fix this, remove references to strings.
                var purgeable = _solutionContainerSystem.SplitSolutionWithout(puddleSolution.Value, purgeAmount, "Water", reagent.ID);

                purgeAmount -= purgeable.Volume;

                _solutionContainerSystem.TryAddSolution(puddleSolution.Value, new Solution("Water", purgeable.Volume));

                if (purgeable.Volume <= FixedPoint2.Zero)
                {
                    break;
                }
            }

            if (canDoDecals)
            {
                var decals = _decalSystem.GetDecalsIntersecting(tile.GridUid,
                    lookupSystem.GetLocalBounds(tile, mapGrid.TileSize)
                        .Enlarged(0.5f)
                        .Translated(new Vector2(-0.5f, -0.5f)));
                foreach (var decal in decals)
                {
                    if (!decal.Decal.Cleanable)
                        continue;

                    _decalSystem.RemoveDecal(tile.GridUid, decal.Index, decalGrid);
                }
            }
        }
    }

    private void Scrub(float timeDelta, ESRemoveGasComponent component, GasMixture tile)
    {
        var transferRate = component.ScrubRate * _atmosphereSystem.PumpSpeedup();
        foreach (var gas in component.GasesToRemove)
        {
            var amountOfGas = tile.GetMoles(gas);
            var amountToReduceBy = timeDelta * transferRate;
            var adjustedAmountOfGas = MathF.Min(0f, amountOfGas - amountToReduceBy);
            tile.AdjustMoles(gas, adjustedAmountOfGas);
        }
    }
}
