using System.Numerics;
using Content.Client.Hands.Systems;
using Content.Shared._ES.Crosshair;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Utility;

namespace Content.Client.CombatMode;

/// <summary>
///     This shows something like crosshairs for the combat mode next to the mouse cursor.
///     If the inhand item provides an entity crosshair with <see cref="ESCrosshairProviderComponent"/>,
///     we don't render a local UI-space one.
/// </summary>
public sealed class CombatModeIndicatorsOverlay : Overlay
{
    private readonly IInputManager _inputManager;
    private readonly IEntityManager _entMan;
    private readonly IEyeManager _eye;
    private readonly CombatModeSystem _combat;
    private readonly HandsSystem _hands;

    private readonly Texture _meleeSight;
    private float _baseScale = 2f;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public CombatModeIndicatorsOverlay(IInputManager input,
        IEntityManager entMan,
            IEyeManager eye,
            CombatModeSystem combatSys,
            HandsSystem hands)
    {
        _inputManager = input;
        _entMan = entMan;
        _eye = eye;
        _combat = combatSys;
        _hands = hands;

        var spriteSys = _entMan.EntitySysManager.GetEntitySystem<SpriteSystem>();
        _meleeSight = spriteSys.Frame0(new SpriteSpecifier.Rsi(new ResPath("/Textures/_ES/Effects/crosshair.rsi"),
             "melee"));
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_combat.IsInCombatMode() || _entMan.HasComponent<ESCrosshairProviderComponent>(_hands.GetActiveHandEntity()))
            return false;

        return base.BeforeDraw(in args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var mouseScreenPosition = _inputManager.MouseScreenPosition;
        var mousePosMap = _eye.PixelToMap(mouseScreenPosition);
        if (mousePosMap.MapId != args.MapId)
            return;

        var mousePos = mouseScreenPosition.Position;
        var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;
        var limitedScale = uiScale > 1.25f ? 1.25f : uiScale;

        DrawSight(_meleeSight, args.ScreenHandle, mousePos, limitedScale);
    }

    private void DrawSight(Texture sight, DrawingHandleScreen screen, Vector2 centerPos, float scale)
    {
        var sightSize = sight.Size * scale * _baseScale;

        screen.SetTransform(Matrix3x2.Identity);
        screen.DrawTextureRect(sight,
            UIBox2.FromDimensions(centerPos - sightSize * 0.5f, sightSize));
    }
}
