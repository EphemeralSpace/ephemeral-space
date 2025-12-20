using Content.Shared._ES.Core.Timer.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Masks.Blessed;

/// <summary>
///     Raised directed after a time on the Blessed's killer when it's their time to die.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ESBlessedKillerTimeToDieEvent : ESEntityTimerEvent;
