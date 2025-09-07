using Content.Shared._ES.Power.Antimatter;
using Robust.Client.Graphics;

namespace Content.Client._ES.Power.Antimatter;

public sealed class ESAntimatterSystem : ESSharedAntimatterSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new ESAntimatterOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<ESAntimatterOverlay>();
    }
}
