using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.Storage;

/// <summary>
/// Prevents using an entity's <see cref="StorageComponent"/> if its <see cref="OpenableComponent"/> is locked.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StorageOpenableComponent : Component;
