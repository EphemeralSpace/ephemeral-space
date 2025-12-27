using Content.Shared.Mobs;

namespace Content.Server._ES.Masks.Objectives.Relays.Components;

/// <summary>
///     When on an entity with a mind, manages relaying mob state changes to the mind.
///     This listens for <see cref="MobStateChangedEvent"/>.
///     Used primarily by <see cref="ESMobStateObjectiveSystem"/>.
/// </summary>
/// <seealso cref="ESMobStateRelaySystem"/>
/// <seealso cref="ESMobStateChanged"/>
[RegisterComponent]
[Access(typeof(ESMobStateRelaySystem))]
public sealed partial class ESMobStateRelayComponent : Component;
