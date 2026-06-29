using Robust.Shared.GameStates;

namespace Content.Shared._ES.Breakable.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESBreakableActivatableUiSystem))]
public sealed partial class ESBreakableActivatableUiComponent : Component;
