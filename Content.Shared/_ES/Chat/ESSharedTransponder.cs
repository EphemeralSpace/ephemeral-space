using Content.Shared.Actions;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat;

public sealed partial class ESTransponderActionEvent : InstantActionEvent
{
    [DataField]
    public ProtoId<RadioChannelPrototype> Channel;
}
