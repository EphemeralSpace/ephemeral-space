using Content.Client.Administration.Managers;
using Content.Client.Ghost;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Robust.Client.Console;
using Robust.Shared.Utility;

namespace Content.Client.Chat.Managers;

internal sealed partial class ChatManager : IChatManager
{
    [Dependency] private IClientConsoleHost _consoleHost = default!;
    [Dependency] private IClientAdminManager _adminMgr = default!;
    [Dependency] private IEntitySystemManager _systems = default!;

    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("chat");
        _sawmill.Level = LogLevel.Info;
    }

    public void SendAdminAlert(string message)
    {
        // See server-side manager. This just exists for shared code.
    }

    public void SendAdminAlert(EntityUid player, string message)
    {
        // See server-side manager. This just exists for shared code.
    }

    public void SendAdminAlertNoFormatOrEscape(string message)
    {
        // See server-side manager. This just exists for shared code.
    }
}
