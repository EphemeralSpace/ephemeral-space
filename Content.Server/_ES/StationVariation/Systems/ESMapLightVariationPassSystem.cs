using System.Linq;
using Content.Server._ES.StationVariation.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.VariationPass;

namespace Content.Server._ES.StationVariation.Systems;

/// <summary>
/// Handles changing the map ambient light on the station map as a variation pass (for variety sauce)
/// </summary>
public sealed class ESMapLightVariationPassSystem : VariationPassSystem<ESMapLightVariationPassComponent>
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    protected override void ApplyVariation(Entity<ESMapLightVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        var grid = Stations.GetLargestGrid(args.Station!);
        if (grid == null)
            return;

        _map.SetAmbientLight(_xform.GetMapId(grid.Value), ent.Comp.Light);
    }
}
