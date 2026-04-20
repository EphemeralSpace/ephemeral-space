namespace Content.Server._ES.Masks.Pickpocket.Components;

[RegisterComponent]
[Access(typeof(ESPickpocketMaskSystem))]
public sealed partial class ESPickpocketStolenComponent : Component
{
    [DataField]
    public HashSet<EntityUid> TargetMinds = new();

    [DataField]
    public HashSet<EntityUid> StealerMinds = new();
}
