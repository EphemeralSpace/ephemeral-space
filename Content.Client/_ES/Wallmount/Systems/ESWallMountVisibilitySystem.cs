using Robust.Client.Graphics;

namespace Content.Client._ES.Wallmount.Systems;

/// <summary>
///     Handles adding and removing the wallmount visibility overlay (which is not an "overlay", really, but)
/// </summary>
public sealed class ESWallMountVisibilitySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new ESWallMountVisibilityOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<ESWallMountVisibilityOverlay>();
    }
}
