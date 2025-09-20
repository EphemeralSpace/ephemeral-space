using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Cargo.Storeroom.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedStoreroomSystem))]
public sealed partial class ESCargoManifestComputerComponent : Component;

[Serializable, NetSerializable]
public enum ESCargoManifestUiKey : byte
{
    Key,
}
