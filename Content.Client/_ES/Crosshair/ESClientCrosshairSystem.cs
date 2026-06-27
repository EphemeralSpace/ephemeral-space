using Content.Shared._ES.Crosshair;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._ES.Crosshair;

/// <summary>
///     Handles occluding crosshairs out of view of the local player as well as raising events if we have a crosshair.
/// </summary>
public sealed class ESClientCrosshairSystem : EntitySystem
{
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<ESCrosshairEntityComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var entity, out var sprite, out var xf orm))
        {
            if (entity.User == _player.LocalEntity)
                _sprite.SetVisible((uid, sprite), true);

        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted || !_input.MouseScreenPosition.IsValid)
            return;

        var player = _player.LocalEntity;

        if (player == null || !TryComp<ESCrosshairAimerComponent>(player, out var aimer))
            return;

        if (aimer.CrosshairEntity == null)
            return;

        var xform = Transform(player.Value);
        var coords = _input.MouseScreenPosition;
        var mapPos = _eye.PixelToMap(coords);

        if (mapPos.MapId == MapId.Nullspace)
            return;

        RaisePredictiveEvent(new ESCrosshairNetworkEvent()
        {
            Coordinates = mapPos,
            User = GetNetEntity(player)
        });
    }
}
