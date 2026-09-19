using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Areas.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ESAreaChunkComponent : Component
{
    [DataField]
    public Dictionary<Vector2i, ProtoId<ESAreaPrototype>> Areas = new();
}
