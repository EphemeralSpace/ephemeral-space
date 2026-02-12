using Content.Shared._ES.KillTracking.Components;

namespace Content.Server._ES.Masks.Objectives.Relays.Components;

/// <summary>
/// Used to relay <see cref="ESPlayerKilledEvent"/>
/// </summary>
[RegisterComponent]
[Access(typeof(ESKilledRelaySystem))]
public sealed partial class ESKilledRelayComponent : Component;
