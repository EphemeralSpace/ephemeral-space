using Robust.Client.Graphics;
using Robust.Shared.Console;

namespace Content.Client._ES.Areas;

public sealed partial class ESAreasOverlaySystem : EntitySystem
{
    [Dependency] private IConsoleHost _consoleHost = default!;
    [Dependency] private IDependencyCollection _deps = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;

    private ESDebugAreaOverlay _overlay = default!;

    private const string ToggleAreasCommand = "toggleareas";

    /// <inheritdoc/>
    public override void Initialize()
    {
        _overlay = new ESDebugAreaOverlay();
        _deps.InjectDependencies(_overlay);
        if (_overlay is IPostInjectInit init)
            init.PostInject();

        _consoleHost.RegisterCommand(ToggleAreasCommand, "Toggles the debug area overlay", "", OnToggleAreaOverlay);
    }

    public override void Shutdown()
    {
        _consoleHost.UnregisterCommand(ToggleAreasCommand);
    }

    private void OnToggleAreaOverlay(IConsoleShell shell, string argStr, string[] args)
    {
        if (_overlayMan.HasOverlay<ESDebugAreaOverlay>())
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
        else
        {
            _overlayMan.AddOverlay(_overlay);
        }
    }
}
