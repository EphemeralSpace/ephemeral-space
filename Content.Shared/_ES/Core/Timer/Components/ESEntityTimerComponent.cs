using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._ES.Core.Timer.Components;

/// <summary>
/// Component that holds data regarding a generic timer.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(ESEntityTimerSystem), Other = AccessPermissions.None)]
public sealed partial class ESEntityTimerComponent : Component
{
    /// <summary>
    /// Event raised on the target entity when the timer ends.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ESEntityTimerEvent TimerEndEvent;

    /// <summary>
    /// Time at which this timer will end.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan TimerEnd;
}

/// <summary>
/// Generic event that all timer events must inherit from
/// </summary>
[Serializable, NetSerializable]
[ImplicitDataDefinitionForInheritors]
public abstract partial class ESEntityTimerEvent : EntityEventArgs;
