using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Wounds;

[RegisterComponent]
public sealed partial class MessageOnHeartstopComponent : Component
{
    [DataField]
    public ProtoId<EmotePrototype>? Message;
}