using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Forensics.Fingerprints.Components;

/// <summary>
/// Component that marks clothing in <see cref="SlotFlags.GLOVES"/> as preventing the transfer of fingerprints.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESFingerprintsSystem))]
public sealed partial class ESFingerprintBlockerComponent : Component;
