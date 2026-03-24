using Content.Shared._ES.Core.Timer.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Masks.Nobleman;

[Serializable, NetSerializable]
public sealed partial class ESTimedDemiseOnKillEvent : ESEntityTimerEvent;
