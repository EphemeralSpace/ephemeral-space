namespace Content.Shared._ES.SecretIdentity.Stalker.Components;

[RegisterComponent]
public sealed partial class ESStalkerTargetComponent : Component
{
    [DataField]
    public EntityUid OwningMind;
}
