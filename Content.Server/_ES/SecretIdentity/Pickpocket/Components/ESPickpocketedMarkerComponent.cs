namespace Content.Server._ES.SecretIdentity.Pickpocket.Components;

/// <summary>
/// Marks an entity as having been pickpocketed by one of the specified minds.
/// </summary>
[RegisterComponent]
[Access(typeof(ESPickpocketSecretIdentitySystem))]
public sealed partial class ESPickpocketedMarkerComponent : Component
{
    [DataField]
    public List<EntityUid> PickpocketMinds = new();
}
