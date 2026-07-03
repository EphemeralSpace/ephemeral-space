using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FlavorProfileComponent : Component
{
    /// <summary>
    ///     Localized string containing the base flavor of this entity.
    /// </summary>
    [DataField]
    public HashSet<string> Flavors { get; private set; } = new();
}
