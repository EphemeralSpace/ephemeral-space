using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Areas.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESAreaChunkComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<Vector2i, ProtoId<ESAreaPrototype>> Areas = new();
}
