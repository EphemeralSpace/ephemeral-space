using Robust.Shared.GameStates;

namespace Content.Shared._ES.Forensics.Fingerprints.Components;

/// <summary>
/// Used to store fingerprints which have been transferred to an object.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESFingerprintsSystem))]
public sealed partial class ESFingerprintEvidenceComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ESFingerprint> Fingerprints = new();
}
