using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.NuBody;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(OFMBodySystem))]
public sealed partial class OFMOrganComponent : Component
{
    /// <summary>
    /// The body entity containing this organ, if any
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Body;
}
