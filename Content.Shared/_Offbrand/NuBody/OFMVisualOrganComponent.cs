using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.NuBody;

[RegisterComponent, NetworkedComponent]
public sealed partial class OFMVisualOrganComponent : Component
{
    /// <summary>
    /// The layer on the entity with <see cref="OFMVisualBodyComponent" /> that this contributes to
    /// </summary>
    [DataField(required: true)]
    public Enum Layer;

    /// <summary>
    /// The data for the layer
    /// </summary>
    [DataField(required: true)]
    public PrototypeLayerData Data;
}
