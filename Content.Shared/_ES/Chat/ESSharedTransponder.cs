using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat;

public sealed partial class ESTransponderActionEvent : InstantActionEvent
{
    [DataField]
    public ProtoId<ESChatChannelPrototype> Channel;
}
