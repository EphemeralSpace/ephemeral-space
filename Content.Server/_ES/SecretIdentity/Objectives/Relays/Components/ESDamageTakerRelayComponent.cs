using Content.Shared.Damage.Systems;

namespace Content.Server._ES.SecretIdentity.Objectives.Relays.Components;

/// <summary>
/// Relays <see cref="DamageChangedEvent"/> for use by objectives
/// </summary>
[RegisterComponent]
[Access(typeof(ESDamageTakerRelaySystem))]
public sealed partial class ESDamageTakerRelayComponent : Component;
