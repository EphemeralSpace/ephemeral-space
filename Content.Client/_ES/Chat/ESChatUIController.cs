using Content.Shared._ES.Chat;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Network;

namespace Content.Client._ES.Chat;

public sealed class ESChatUIController : UIController
{
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        _net.RegisterNetMessage<ESChatNetMessage>(OnChatMessage);
    }

    private void OnChatMessage(ESChatNetMessage msg)
    {
        var message = msg.Message;
        Log.Debug(string.Format(message.Format, message.Content, message.Name ?? string.Empty));
    }
}
