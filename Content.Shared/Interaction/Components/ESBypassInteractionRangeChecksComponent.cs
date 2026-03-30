using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Components;

/// <summary>
///     Marks an entity as not caring about interaction limitations, like walls or not fitting criteria.
///     This is primarily used on admin ghosts.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESBypassInteractionRangeChecksComponent : Component;
