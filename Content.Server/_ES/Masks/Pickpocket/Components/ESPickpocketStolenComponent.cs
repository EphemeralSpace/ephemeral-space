namespace Content.Server._ES.Masks.Pickpocket.Components;

[RegisterComponent]
[Access(typeof(ESPickpocketMaskSystem), typeof(ESHoldPickpocketedObjectiveSystem))]
public sealed partial class ESPickpocketStolenComponent : Component
{
    [DataField]
    public HashSet<EntityUid> StolenMinds = new();

    [DataField]
    public HashSet<EntityUid> StealerMinds = new();
}
