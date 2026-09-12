using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Forensics.Fingerprints.Components;

/// <summary>
/// Object which can be used on entities with <see cref="ESFingerprintEvidenceComponent"/> to
/// produce a <see cref="ESFingerprintCardComponent"/> with the fingerprints which were left on it.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESFingerprintsSystem))]
public sealed partial class ESFingerprintKitComponent : Component
{
    /// <summary>
    /// How long it takes to dust fingerprints off of a target
    /// </summary>
    [DataField]
    public TimeSpan DustTime = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The fingerprint card that is created after dusting
    /// </summary>
    [DataField]
    public EntProtoId<ESFingerprintCardComponent> CardPrototype = "ESCardFingerprint";
}

[Serializable, NetSerializable]
public sealed partial class ESDustFingerprintsDoAfterEvent : SimpleDoAfterEvent;
