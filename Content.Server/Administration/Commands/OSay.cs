using System.Linq;
using Content.Server._ES.Chat;
using Content.Server.Administration.Logs;
using Content.Shared._ES.Chat;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class OSay : LocalizedCommands
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    public override string Command => "osay";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString("osay-command-arg-uid"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(CompletionHelper.PrototypeIDs<ESChatChannelPrototype>(),
                Loc.GetString("osay-command-arg-type"));
        }

        if (args.Length > 2)
        {
            return CompletionResult.FromHint(Loc.GetString("osay-command-arg-message"));
        }

        return CompletionResult.Empty;
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 3)
        {
            shell.WriteLine(Loc.GetString("osay-command-error-args"));
            return;
        }

        if (!_prototype.TryIndex<ESChatChannelPrototype>(args[1], out var channel))
        {
            return;
        }

        if (!NetEntity.TryParse(args[0], out var sourceNet) || !_entityManager.TryGetEntity(sourceNet, out var source) || !_entityManager.EntityExists(source))
        {
            shell.WriteLine(Loc.GetString("osay-command-error-euid", ("arg", args[0])));
            return;
        }

        var message = string.Join(" ", args.Skip(2)).Trim();
        if (string.IsNullOrEmpty(message))
            return;

        _entityManager.System<ESChatSystem>().TrySendMessage(message, channel, source.Value);
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{(shell.Player != null ? shell.Player.Name : "An administrator")} forced {_entityManager.ToPrettyString(source.Value)} to {args[1]}: {message}");
    }
}
