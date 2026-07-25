using System.Linq;
using Content.Server._ES.WarpDrive;
using Content.Shared._ES.SecretIdentity.WarpDisciple;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;

namespace Content.Server._ES.SecretIdentity.WarpDisciple;

public sealed partial class ESTeleportToWarpActionSystem : EntitySystem
{
    [Dependency] private ESWarpDriveSystem _warp = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ESTeleportToWarpInstantAction>(OnTeleportWarp);
    }

    private void OnTeleportWarp(ESTeleportToWarpInstantAction args)
    {
        if (TryComp<PullableComponent>(args.Performer, out var pull) && _pulling.IsPulled(args.Performer, pull))
            _pulling.TryStopPull(args.Performer, pull);

        if (TryComp<PullerComponent>(args.Performer, out var puller) && TryComp<PullableComponent>(puller.Pulling, out var pullable))
            _pulling.TryStopPull(puller.Pulling.Value, pullable);

        var grids = _warp.GetSingularityWorldGrids();

        if (grids == null)
        {
            Log.Error("No singularity world grids found");
            return;
        }

        _warp.TryTeleportToWarp(TimeSpan.FromSeconds(40), args.Performer);

        var targetGrid = grids.First();
        var coords = Transform(targetGrid).Coordinates;
        _transform.SetCoordinates(args.Performer, coords);
        _transform.AttachToGridOrMap(args.Performer);

        args.Handled = true;
    }
}
