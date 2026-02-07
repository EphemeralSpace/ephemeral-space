using Content.Shared._ES.Core.Timer.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Mind;

[Serializable, NetSerializable]
public sealed partial class ESAutoGhostEvent : ESEntityTimerEvent;
