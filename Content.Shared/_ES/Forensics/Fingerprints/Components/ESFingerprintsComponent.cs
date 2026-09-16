using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Forensics.Fingerprints.Components;

/// <summary>
/// Denotes an entity as having fingerprints which they can leave on objects.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESFingerprintsSystem))]
public sealed partial class ESFingerprintsComponent : Component
{
    /// <summary>
    /// The unique fingerprint associated with this entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public ESFingerprint Fingerprint;
}

/// <summary>
/// Data object that represents a fingerprint.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public partial record struct ESFingerprint(int Id)
{
    [DataField]
    public int Id = Id;
}
