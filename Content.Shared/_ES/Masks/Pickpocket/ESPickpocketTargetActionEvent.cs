using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Masks.Pickpocket;

public sealed partial class ESPickpocketTargetActionEvent : EntityTargetActionEvent
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3);
}

[Serializable, NetSerializable]
public sealed partial class ESPickpocketTargetDoAfterEvent : SimpleDoAfterEvent;
