using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Stagehand.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ESStagehandComponent : Component;

[Serializable, NetSerializable]
public enum ESStagehandUiKey : byte
{
    Observe,
}

[Serializable, NetSerializable]
public sealed class ESWarpToMindMessage : BoundUserInterfaceMessage
{
    public NetEntity Mind;
}
