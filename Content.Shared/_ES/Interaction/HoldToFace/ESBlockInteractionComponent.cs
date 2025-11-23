using Robust.Shared.GameStates;

namespace Content.Shared._ES.Interaction.HoldToFace;

/// <summary>
/// Blocks all interactions
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESBlockInteractionSystem))]
public sealed partial class ESBlockInteractionComponent : Component;
