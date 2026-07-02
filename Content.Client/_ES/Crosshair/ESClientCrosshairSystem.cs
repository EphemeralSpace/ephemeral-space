using System.Numerics;
using Content.Client.UserInterface.Systems.Chat;
using Content.Shared._ES.Crosshair;
using Content.Shared._ES.Viewcone;
using Content.Shared.Examine;
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
    [Dependency] private ExamineSystemShared _occluder = default!;
    [Dependency] private ESViewconeAngleSystem _viewcone = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private const float LerpHalfLife = 0.025f;

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
                _sprite.LayerSetVisible((uid, sprite), ESCrosshairVisualLayers.Crosshair, false);
                continue;
            }

            // lerp and offset
            // we do this clientside and per frame to not fuck up prediction
            // we could do this by drawing in an overlay but im using sprite offset because whatever
            var actualPos = _xform.GetWorldPosition(xform);
            entity.LerpPos ??= actualPos;
            entity.LerpPos = Vector2.Lerp(entity.LerpPos.Value, actualPos, 1f - MathF.Pow(2f, -(frameTime / LerpHalfLife)));
            var eyeRot = _eye.CurrentEye.Rotation;
            _sprite.SetOffset((uid, sprite), eyeRot.RotateVec(entity.LerpPos.Value - actualPos));

            if (user == _player.LocalEntity)
            {
                _sprite.LayerSetVisible((uid, sprite), ESCrosshairVisualLayers.Crosshair, true);
                continue;
            }

            // check if the user of the crosshair is occluded
            if (!_occluder.InRangeUnOccluded(user, playerPos) || !_viewcone.InViewcone(playerEnt, user))
            {
                _sprite.LayerSetVisible((uid, sprite), ESCrosshairVisualLayers.Crosshair, false);
                continue;
            }

            _sprite.LayerSetVisible((uid, sprite), ESCrosshairVisualLayers.Crosshair, true);
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

        var coords = _input.MouseScreenPosition;
        var mousePos = _eye.PixelToMap(coords);

        if (aimer.CrosshairEntity == null)
            return;

        var pos = _xform.GetMapCoordinates(aimer.CrosshairEntity.Value);
        if (mousePos.Position.EqualsApprox(pos.Position, 0.01d))
            return;

        if (mousePos.MapId == MapId.Nullspace)
            return;

        RaisePredictiveEvent(new ESCrosshairNetworkEvent()
        {
            Coordinates = mousePos,
        });
    }
}

public enum ESCrosshairVisualLayers : byte
{
    Crosshair
}
