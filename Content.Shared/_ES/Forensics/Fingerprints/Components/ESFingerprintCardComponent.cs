using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Forensics.Fingerprints.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESFingerprintsSystem))]
public sealed partial class ESFingerprintCardComponent : Component
{
    [ViewVariables]
    public bool Used => Fingerprints.Count != 0;

    [DataField, AutoNetworkedField]
    public List<ESFingerprint> Fingerprints = new();
}

[Serializable, NetSerializable]
public enum ESFingerprintCardVisuals : byte
{
    Used,
}
