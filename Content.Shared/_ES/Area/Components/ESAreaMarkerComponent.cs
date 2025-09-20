using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Area.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedAreaSystem))]
public sealed partial class ESAreaMarkerComponent : Component
{
    /// <summary>
    /// Corresponding area
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ESAreaPrototype> Area;
}
