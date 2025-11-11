using Robust.Shared.Serialization;

namespace Content.Shared._ES.Stagehand;

[Serializable, NetSerializable]
public sealed class ESJoinStagehandMessage : EntityEventArgs;

[Serializable, NetSerializable]
public enum ESJoinStagehandUiKey : byte
{
    Key,
}
