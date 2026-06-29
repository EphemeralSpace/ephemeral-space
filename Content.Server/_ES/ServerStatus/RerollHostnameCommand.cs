using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._ES.ServerStatus;

[ToolshedCommand]
[AdminCommand(AdminFlags.Fun)]
public sealed partial class RerollHostnameCommand : ToolshedCommand
{
    [Dependency] private StatusManager _status = default!;

    [CommandImplementation]
    public void RerollHostname()
    {
        _status.RerollHostname();
    }
}
