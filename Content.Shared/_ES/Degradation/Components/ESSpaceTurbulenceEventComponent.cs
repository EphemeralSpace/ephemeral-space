using Content.Shared._ES.Core.Timer.Components;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Degradation.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ESSpaceTurbulenceEventComponent : Component
{
    [DataField]
    public MinMax JerkRange = new(3, 5);

    [DataField]
    public TimeSpan MinJerkDelay = TimeSpan.FromSeconds(2.5);

    [DataField]
    public TimeSpan MaxJerkDelay = TimeSpan.FromSeconds(7);

    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(3);

    [DataField]
    public float ThrowDistance = 15;

    [DataField]
    public float ThrowForce = 10f;
}

[Serializable, NetSerializable]
public sealed partial class ESSpaceTurbulenceJerkTimerEvent : ESEntityTimerEvent;
