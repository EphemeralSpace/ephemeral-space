using Content.Shared._ES.KillTracking.Components;

namespace Content.Server._ES.SecretIdentity.Objectives.Relays.Components;

/// <summary>
/// Used to relay <see cref="ESPlayerKilledEvent"/>
/// </summary>
[RegisterComponent]
[Access(typeof(ESKilledRelaySystem))]
public sealed partial class ESKilledRelayComponent : Component;
