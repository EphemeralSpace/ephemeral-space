using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Toolshed;

namespace Content.Server._ES.Debugging.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed partial class ToastCommand : ToolshedCommand
{
    [Dependency] private IConfigurationManager _cfg = default!;
    private GameTicker? _ticker;

    [CommandImplementation]
    public void Toast()
    {
        _ticker ??= GetSys<GameTicker>();
        // this is kind of stupid but whatever its what forcemap does
        _cfg.SetCVar(CCVars.GameMap, "ESToast");
        _ticker.RestartRound();
    }
}
