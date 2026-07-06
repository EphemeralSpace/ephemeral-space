using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._ES.DeathCutscene;

public sealed partial class ESDeathCutsceneOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "Noir";
    private static readonly TimeSpan TimeUntilMaxIntensity = TimeSpan.FromSeconds(10);

    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _shader;
    private TimeSpan _startTime;

    public ESDeathCutsceneOverlay(TimeSpan startTime)
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index(Shader).InstanceUnique();
        _startTime = startTime;
        ZIndex = 9;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var blend = (float)Math.Clamp((_timing.RealTime - _startTime).TotalSeconds / TimeUntilMaxIntensity.TotalSeconds, 0.0f, 1.0f);
        var intensity = MathHelper.Lerp(0f, 1f, Easings.OutSine(blend));
        var handle = args.WorldHandle;
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("intensity", intensity);
        _shader.SetParameter("noise_intensity", intensity);
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
