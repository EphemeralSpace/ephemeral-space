using Robust.Shared.GameStates;

namespace Content.Shared._ES.Cargo.Storeroom.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(ESSharedStoreroomSystem))]
public sealed partial class ESStoreroomStationComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<ESStoreroomContainerEntry, int> Stock = new();
}
