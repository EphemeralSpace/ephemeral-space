using Content.Shared.Humanoid.Markings;
using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.NuBody;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class OFMVisualOrganMarkingsComponent : Component
{
    /// <summary>
    /// The layers on the entity that this can contain markings for
    /// </summary>
    [DataField(required: true)]
    public HashSet<Enum> Layers;

    /// <summary>
    /// The list of markings to apply to the entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<Marking> Markings = new();

    /// <summary>
    /// Client only - the last markings applied by this component
    /// </summary>
    public List<Marking> AppliedMarkings = new();
}
