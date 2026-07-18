using Content.Server._ES.SpawnRegion;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.VariationPass;
using Content.Shared._ES.SpawnRegion;
using Content.Shared.Physics;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Ratman;

/// <summary>
///     Handles replacing anchored entities which are marked around the station with a set number of ratman doors.
/// </summary>
public sealed partial class ESRatmanDoorVariationPassSystem : VariationPassSystem<ESRatmanDoorVariationPassComponent>
{
    [Dependency] private ESSpawnRegionSystem _region = default!;
    [Dependency] private SharedMapSystem _map = default!;

    private static readonly ProtoId<ESSpawnRegionPrototype> Region = "ESRatmanDoor";

    protected override void ApplyVariation(Entity<ESRatmanDoorVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        for (var i = 0; i < ent.Comp.Count; i++)
        {
            if (!_region.TryGetRandomCoordsInRegion(Region,
                    args.Station.AsNullable(),
                    out var coords,
                    blockLayer: CollisionGroup.None,
                    false,
                    3.5f,
                    false,
                    false))
                continue;

            var grid = coords.Value.EntityId;
            if (!TryComp<MapGridComponent>(grid, out var gridComp))
                continue;

            var ents = _map.GetAnchoredEntities((grid, gridComp), coords.Value);
            foreach (var replacingEnts in ents)
            {
                QueueDel(replacingEnts);
            }

            SpawnAtPosition(ent.Comp.Replacement, coords.Value);
        }
    }
}
