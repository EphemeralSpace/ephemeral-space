using System.Numerics;
using Content.Shared.Throwing;
using Content.Shared.Wall;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._ES.Wallmount;

/// <summary>
/// This handles only showing wallmounts which are on "sides of the wall" that your eye can see,
/// by checking their direction and rotation & comparing to their position relative to your eye.
/// </summary>
public sealed class ESWallmountVisibilitySystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity == null)
            return;

        var playerXform = Transform(_player.LocalEntity.Value);
        if (playerXform.MapID != _eye.CurrentEye.Position.MapId)
            return;

        var playerCoords = _eye.MapToScreen(_xform.GetMapCoordinates(playerXform));
        if (!playerCoords.IsValid)
            return;

        var enumerator = AllEntityQuery<WallMountComponent, SpriteComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var wallmount, out var sprite, out var xform))
        {
            if (xform.MapID != _eye.CurrentEye.Position.MapId)
                continue;

            if (!_eye.CurrentEye.DrawFov)
            {
                _sprite.SetVisible((uid, sprite), true);
                continue;
            }

            // should just be visible if the arc is 360 deg
            // (oh god windows have wallmount)
            if (wallmount.Arc >= Math.Tau)
                continue;

            var screenCoords = _sprite.GetSpriteScreenCoordinates((uid, sprite, xform));
            if (!screenCoords.IsValid)
                continue;

            // we figure out which wallmounts should be visible based on their direction & rotation adjusted for eye rotation
            // + their position relative to the player's screencoords (the four quadrants surrounding them), which is
            // usually the center of the screen but doesn't necessarily need to be
            var wallmountScreenRotation = _xform.GetWorldRotation(xform) + _eye.CurrentEye.Rotation + wallmount.Direction;

            // measure how much the wallmount angle is 'facing' the player
            // if its < 90deg then it should be visible

            var dist = (screenCoords.Position - playerCoords.Position);
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
