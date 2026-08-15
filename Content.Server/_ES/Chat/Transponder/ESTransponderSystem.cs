using Content.Server.Actions;
using Content.Server.Administration;
using Content.Shared._ES.Chat;
using Content.Shared.Actions.Components;
using Robust.Server.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Chat.Transponder;

/// <summary>
///     Sends a message to a radio channel, outside of the regular chat paradigm, using an action.
/// </summary>
public sealed partial class ESTransponderSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private ESSharedChatSystem _chat = default!;
    [Dependency] private QuickDialogSystem _dialog = default!;
    [Dependency] private ActionsSystem _action = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESTransponderActionEvent>(OnUseTransponder);
    }

    private void OnUseTransponder(ESTransponderActionEvent args)
    {
        if (!_player.TryGetSessionByEntity(args.Performer, out var session))
            return;

        var uid = args.Performer;
        var channel = args.Channel;
        var action = args.Action;
        _dialog.OpenDialog<string>(session,
            Loc.GetString("es-transponder-dialog-title"),
            Loc.GetString("es-transponder-dialog-prompt"),
            (msg => SendMessage(uid, channel, msg, action)));

        // we deliberately do not set handled, because we activate the usedelay when a message is actually sent
    }

    private void SendMessage(EntityUid ent, ProtoId<ESChatChannelPrototype> channel, string message, Entity<ActionComponent> action)
    {
        if (_action.IsCooldownActive(action))
            return;

        // send chat msg
        _chat.TrySendMessage(message, channel, ent);
        _action.StartUseDelay(action.AsNullable());
    }
}
