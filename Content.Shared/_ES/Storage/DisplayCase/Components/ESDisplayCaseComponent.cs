using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Storage.DisplayCase.Components;

/// <summary>
/// Used for a breakable container which
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESDisplayCaseSystem))]
public sealed partial class ESDisplayCaseComponent : Component
{
    /// <summary>
    /// The primary container for the display case
    /// </summary>
    [DataField]
    public string ItemSlotId = "display-case";

    /// <summary>
    /// Whether this case has been broken, exposing what's inside of it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Broken;
}

[Serializable, NetSerializable]
public enum ESDisplayCaseVisuals
{
    Broken,
}
