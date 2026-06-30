using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.SecretIdentity.Traitor.Events;

[Serializable, NetSerializable]
public sealed partial class ESAddMaskOnUseDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
