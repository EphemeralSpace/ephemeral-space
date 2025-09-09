using System.Numerics;
using Content.Client._ES.Wallmount.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._ES.Wallmount;

/// <summary>
///     Renders wallmount visibility based on their facing direction and position relative to the center of a viewport's eye.
///     This abuses the fact that sprite render commands (like setting visibility) are not batched in any way, and we can
///     just set the visibility to something else mid-render
/// </summary>
public sealed class ESWallMountVisibilityOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _ent = default!;
    private readonly TransformSystem _xform;
    private readonly SpriteSystem _sprite;
    private readonly ESWallMountTreeSystem _tree;

    public ESWallMountVisibilityOverlay()
    {
        IoCManager.InjectDependencies(this);

        _xform  = _ent.EntitySysManager.GetEntitySystem<TransformSystem>();
        _sprite = _ent.EntitySysManager.GetEntitySystem<SpriteSystem>();
        _tree   = _ent.EntitySysManager.GetEntitySystem<ESWallMountTreeSystem>();
    }

    // b4 entities so we can modify their visibility and such
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null)
            return;

        var matrix = args.Viewport.GetWorldToLocalMatrix();
        var entities = _tree.QueryAabb(args.MapId, args.WorldBounds);

        foreach (var entry in entities)
        {
            var (wallmount, xform) = entry;
            var uid = entry.Uid; // this uses component.Owner.. oh well

            if (!_ent.TryGetComponent<SpriteComponent>(uid, out var sprite))
                continue;

            if (!args.Viewport.Eye.DrawFov)
            {
                _sprite.SetVisible((uid, sprite), true);
                continue;
            }

            // shouldnt be here in the query to begin with bc of addtotree check but if it is we ignore it
            if (wallmount.Arc >= Math.Tau)
                continue;

            var (pos, rot) = _xform.GetWorldPositionRotation(xform);

            // we figure out which wallmounts should be visible based on their direction & rotation adjusted for eye rotation
            // + their position relative to the viewport center's screencoords (the four quadrants surrounding them)
            var wallmountScreenRotation = rot + args.Viewport.Eye.Rotation + wallmount.Direction;

            var entityScreenPos = Vector2.Transform(pos, matrix);
            var eyeScreenPos = Vector2.Transform(args.Viewport.Eye.Position.Position, matrix); // there is surely a better way to get this value from somewhere
            var dist = (entityScreenPos - eyeScreenPos);

            // measure how much the wallmount angle is 'facing' the viewport center
            // if its < 90deg then it should be visible
            // i have no fucking idea why i need to flip x, genuinely
            // but it fixes the math. it worked fine vertically
            var distAngle = (dist with { X = -dist.X }).ToWorldAngle();
            var angleBetween = Angle.ShortestDistance(distAngle, wallmountScreenRotation);
            var visible = angleBetween > -MathHelper.PiOver2 && angleBetween < MathHelper.PiOver2;
            //Log.Info($"wallmount {Name(uid)} screenrot {wallmountScreenRotation.Degrees} distangle {distAngle.Degrees} anglebetween {angleBetween.Degrees}");

            _sprite.SetVisible((uid, sprite), visible);
        }
    }
}
