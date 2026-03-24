using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Console;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;

namespace Content.Client._ES.UI;

public sealed class SpawnWindowCommand : IConsoleCommand
{
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Dependency] private readonly ISandboxHelper _sandboxHelper = default!;

    public string Command => "spawnwindow";
    public string Description => "Spawns a window of the given type";
    public string Help => "spawnwindow <type>";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var ty = _reflectionManager.LooseGetType(string.Join(" ", args));

        if (ty.IsAssignableTo(typeof(BaseWindow)))
        {
            var window = (BaseWindow)_sandboxHelper.CreateInstance(ty);
            window.OpenCentered();
        }
        else
        {
            throw new Exception($"{ty} is not assignable to BaseWindow");
        }
    }
}
