using Robust.Shared.GameStates;

namespace Content.Shared._ES.Objectives.Components;

/// <summary>
/// Holds UI data for an objective
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(ESSharedObjectiveSystem))]
public sealed partial class ESObjectiveDescriptorComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Text;

    [DataField, AutoNetworkedField]
    public Color Color;

    [DataField, AutoNetworkedField]
    public string Tooltip;
}
