using Content.Server.KillTracking;

namespace Content.Server._ES.Masks.Objectives.Relays.Components;

/// <summary>
/// Used to relay <see cref="KillReportedEvent"/>
/// </summary>
[RegisterComponent]
[Access(typeof(ESKilledRelaySystem))]
public sealed partial class ESKilledRelayComponent : Component;
