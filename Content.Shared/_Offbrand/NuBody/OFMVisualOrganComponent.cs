using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.NuBody;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class OFMVisualOrganComponent : Component
{
    /// <summary>
    /// The layer on the entity that this contributes to
    /// </summary>
    [DataField(required: true)]
    public Enum Layer;

    /// <summary>
    /// The data for the layer
    /// </summary>
    [DataField(required: true), AutoNetworkedField, AlwaysPushInheritance]
    public PrototypeLayerData Data;
}
