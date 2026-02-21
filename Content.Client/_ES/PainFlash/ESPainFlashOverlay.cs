using Content.Shared.FixedPoint;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Client._ES.PainFlash;

public sealed class ESPainFlashOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => false;

    private const float MaxPain = 150;

    private float _painAccumulator;

    public void ResetPainAccumulator()
    {
        _painAccumulator = 0;
    }

    public void SetPainAccumulator(FixedPoint2 inPain)
    {
        _painAccumulator = Math.Min(Math.Max(_painAccumulator, inPain.Float()), MaxPain);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_painAccumulator <= 0)
            return;

        _painAccumulator = _painAccumulator - args.DeltaSeconds * 45;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_painAccumulator <= 0)
            return;

        var handle = args.WorldHandle;

        var blend = Math.Clamp(_painAccumulator / MaxPain, 0, 1);
        var alpha = MathHelper.Lerp(0f, 0.9f, blend);
        var color = Color.Red.WithAlpha(alpha);

        handle.DrawRect(args.WorldBounds, color);
    }
}
