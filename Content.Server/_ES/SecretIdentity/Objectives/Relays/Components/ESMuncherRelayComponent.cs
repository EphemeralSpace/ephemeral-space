using Content.Server._ES.SecretIdentity.Objectives.Components;
using Content.Shared.Nutrition;

namespace Content.Server._ES.SecretIdentity.Objectives.Relays.Components;

/// <summary>
///     When on an entity with a mind, manages relaying their eating and drinking to the mind.
///     This listens for <see cref="IngestingEvent"/> and <see cref="FullyAteEvent"/>, and relays them to the mind.<br/>
///     Some things that use this are <see cref="ESGuzzleObjectiveSystem"/> and <see cref="ESImbibeReagentObjectiveComponent"/>.
/// </summary>
/// <seealso cref="ESMuncherRelaySystem"/>
[RegisterComponent]
[Access(typeof(ESMuncherRelaySystem))]
public sealed partial class ESMuncherRelayComponent : Component;
