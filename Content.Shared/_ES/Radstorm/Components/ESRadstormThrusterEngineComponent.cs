using Content.Shared._ES.Core.Timer.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._ES.Radstorm.Components;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(ESRadstormThrusterEngineSystem))]
public sealed partial class ESRadstormThrusterEngineComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(1);

    [DataField]
    public string FuelTankSolutionId = "fuel-tank";

    [DataField]
    public FixedPoint2 MinStartingFuel = 480;

    [DataField]
    public FixedPoint2 MaxStartingFuel = 1500;

    [DataField]
    public FixedPoint2 FuelConsumptionRate = 0.8f;

    [DataField]
    public ProtoId<ReagentPrototype> FuelReagent = "WeldingFuel";

    [DataField]
    public bool HasFuel = true;

    [DataField]
    public TimeSpan NoFuelDelay = TimeSpan.FromSeconds(30);
}

[ByRefEvent]
public readonly record struct ESThrusterEngineFuelStateChangedEvent(bool HasFuel);

[Serializable, NetSerializable]
public sealed partial class ESThrusterEngineNoFuelTimerEvent : ESEntityTimerEvent;
