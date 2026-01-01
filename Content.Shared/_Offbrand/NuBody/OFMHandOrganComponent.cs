using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.NuBody;

[RegisterComponent, NetworkedComponent]
[Access(typeof(OFMHandOrganSystem))]
public sealed partial class OFMHandOrganComponent : Component
{
    [DataField(required: true)]
    public string HandID;

    [DataField(required: true)]
    public Hand Data;
}
