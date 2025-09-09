using System.Numerics;
using Content.Client._ES.Wallmount.Systems;
using Content.Shared.Wall;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._ES.Wallmount;

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
        if (args.ViewportControl == null || args.Viewport.Eye == null)
            return;

        var matrix = args.ViewportControl.GetWorldToScreenMatrix();
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
            // + their position relative to the player's screencoords (the four quadrants surrounding them), which is
            // usually the center of the screen but doesn't necessarily need to be
            var wallmountScreenRotation = rot + args.Viewport.Eye.Rotation + wallmount.Direction;

            // measure how much the wallmount angle is 'facing' the player
            // if its < 90deg then it should be visible

            var entityScreenPos = Vector2.Transform(pos, matrix);
            var viewportCenterScreenPos = args.ViewportBounds.Center;

            var dist = (entityScreenPos - viewportCenterScreenPos);
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
