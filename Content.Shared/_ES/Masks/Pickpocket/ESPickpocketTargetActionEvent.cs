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
public sealed partial class ESPickpocketTargetDoAfterEvent : SimpleDoAfterEvent
{
    /// <summary>
    /// Chance that the pickpocket will take from the priority item pool instead of just the random one
    /// </summary>
    [DataField]
    public float PriorityItemChance = 0.60f;
}
