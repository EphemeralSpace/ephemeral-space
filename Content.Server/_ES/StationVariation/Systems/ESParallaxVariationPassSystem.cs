using Content.Server._ES.StationVariation.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.VariationPass;
using Content.Shared.Parallax;

namespace Content.Server._ES.StationVariation.Systems;

/// <summary>
/// Handles changing the parallax on the station map as a variation pass (for sauce)
/// </summary>
public sealed class ESParallaxVariationPassSystem : VariationPassSystem<ESParallaxVariationPassComponent>
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    protected override void ApplyVariation(Entity<ESParallaxVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        var grid = Stations.GetLargestGrid(args.Station!);
        if (grid == null || _xform.GetMap(grid.Value) is not { } map)
            return;

        var parallax = EnsureComp<ParallaxComponent>(map);
        parallax.Parallax = ent.Comp.Parallax;
        Dirty(map, parallax);
    }
}
