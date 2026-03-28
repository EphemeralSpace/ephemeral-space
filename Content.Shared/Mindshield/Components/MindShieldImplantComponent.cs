using Robust.Shared.GameStates;

namespace Content.Shared.Mindshield.Components;

/// <summary>
/// Component given to an entity to mark it is a mindshield implant.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MindShieldImplantComponent : Component;
