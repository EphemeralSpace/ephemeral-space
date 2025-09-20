using Robust.Shared.GameStates;

namespace Content.Shared._ES.Cargo.Storeroom.Components;

/// <summary>
/// Used for tracking items on <see cref="ESStoreroomPalletComponent"/>, for use when updating UI.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedStoreroomSystem))]
public sealed partial class ESPalletTrackerComponent : Component;
