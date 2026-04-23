namespace Content.Server._ES.Masks.Pickpocket.Components;

/// <summary>
/// Marks an entity as having been pickpocketed by one of the specified minds.
/// </summary>
[RegisterComponent]
[Access(typeof(ESPickpocketMaskSystem))]
public sealed partial class ESPickpocketedMarkerComponent : Component
{
    [DataField]
    public List<EntityUid> PickpocketMinds = new();
}
