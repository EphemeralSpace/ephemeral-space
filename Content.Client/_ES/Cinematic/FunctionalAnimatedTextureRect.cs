using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._ES.Cinematic;

/// <summary>
///     A more complex control wrapping <see cref="TextureRect"/> that can do RSI directions and animations.
///     That actually works because animatedtexturerect in engine doesnt
/// </summary>
public sealed class FunctionalAnimatedTextureRect : Control
{
    private IRsiStateLike? _state;
    private int _curFrame;
    private float _curFrameTime;

    /// <summary>
    ///     Internal TextureRect used to do actual drawing of the texture.
    ///     You can use this property to change shaders or styling or such.
    /// </summary>
    public TextureRect DisplayRect { get; }

    public RsiDirection RsiDirection { get; } = RsiDirection.South;

    public FunctionalAnimatedTextureRect()
    {
        IoCManager.InjectDependencies(this);

        DisplayRect = new TextureRect();
        AddChild(DisplayRect);
    }

    public void SetFromSpriteSpecifier(SpriteSpecifier specifier)
    {
        _curFrame = 0;
        _state = specifier.RsiStateLike();
        _curFrameTime = _state.GetDelay(0);
        DisplayRect.Texture = _state.GetFrame(RsiDirection, 0);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        if (!VisibleInTree || _state == null || !_state.IsAnimated)
            return;

        var oldFrame = _curFrame;

        _curFrameTime -= args.DeltaSeconds;
        if (_curFrameTime < 0)
        {
            _curFrame = (_curFrame + 1) % _state.AnimationFrameCount;
            _curFrameTime += _state.GetDelay(_curFrame);
        }

        if (_curFrame != oldFrame)
        {
            DisplayRect.Texture = _state.GetFrame(RsiDirection, _curFrame);
        }
    }
}
