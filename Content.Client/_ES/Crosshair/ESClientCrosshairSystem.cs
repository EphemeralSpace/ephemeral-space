using Content.Client.UserInterface.Systems.Chat;
using Content.Shared._ES.Crosshair;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._ES.Crosshair;

/// <summary>
///     Handles occluding crosshairs out of view of the local player as well as raising events if we have a crosshair.
/// </summary>
public sealed partial class ESClientCrosshairSystem : EntitySystem
{
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private OccluderSystem _occluder = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESCrosshairEntityComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(Entity<ESCrosshairEntityComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is null || !args.AppearanceData.TryGetValue(ESCrosshairVisuals.Name, out var obj) || obj is not string name)
            return;

        var controller = _ui.GetUIController<ChatUIController>();
        _sprite.SetColor((ent.Owner, args.Sprite), controller.GetNameColor(name));
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_player.LocalEntity is not { } playerEnt)
            return;

        var playerXform = Transform(playerEnt);
        var playerPos = _xform.GetMapCoordinates(playerXform);
        var query = EntityQueryEnumerator<ESCrosshairEntityComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var entity, out var sprite, out var xform))
        {
            if (entity.User is not { } user)
            {
                _sprite.SetVisible((uid, sprite), false);
                continue;
            }

            if (user == _player.LocalEntity)
            {
                _sprite.SetVisible((uid, sprite), true);
                continue;
            }

            // check if the crosshair itself is occluded
            var entPos = _xform.GetMapCoordinates(xform);
            var entOccluded = _occluder.InRangeUnoccluded(playerPos, entPos, 10f, true);
            if (entOccluded)
            {
                _sprite.SetVisible((uid, sprite), false);
                continue;
            }

            // check if the user of the crosshair is occluded
            var userPos = _xform.GetMapCoordinates(user);
            var userOccluded = _occluder.InRangeUnoccluded(playerPos, userPos, 10f, true);
            if (userOccluded)
            {
                _sprite.SetVisible((uid, sprite), false);
                continue;
            }

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

        var coords = _input.MouseScreenPosition;
        var mapPos = _eye.PixelToMap(coords);

        if (mapPos.MapId == MapId.Nullspace)
            return;

        RaisePredictiveEvent(new ESCrosshairNetworkEvent()
        {
            Coordinates = mapPos,
        });
    }
}
