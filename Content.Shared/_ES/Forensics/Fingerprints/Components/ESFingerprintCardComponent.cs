using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Forensics.Fingerprints.Components;

/// <summary>
/// Used for a specific card that holds fingerprints and can be used to compare prints or analyze them in a machine
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESFingerprintsSystem))]
public sealed partial class ESFingerprintCardComponent : Component
{
    /// <summary>
    /// Used cards have some fingerprints on them
    /// </summary>
    [ViewVariables]
    public bool Used => Fingerprints.Count != 0;

    /// <summary>
    /// What fingerprints are on this card?
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ESFingerprint> Fingerprints = new();
}

[Serializable, NetSerializable]
public enum ESFingerprintCardVisuals : byte
{
    Used,
}
