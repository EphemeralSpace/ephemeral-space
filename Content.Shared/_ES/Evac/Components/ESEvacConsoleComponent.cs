using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Evac.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedEvacSystem))]
public sealed partial class ESEvacConsoleComponent : Component;

[Serializable, NetSerializable]
public sealed class ESToggleStationEvacMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public enum ESEvacUiKey : byte
{
    Key,
}
