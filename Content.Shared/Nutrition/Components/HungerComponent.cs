using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Shared.Nutrition.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(HungerSystem))]
[AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class HungerComponent : Component
{
    /// <summary>
    /// The current hunger threshold the entity is at
    /// </summary>
    [DataField, AutoNetworkedField]
    public HungerThreshold CurrentHunger = HungerThreshold.Okay;

    /// <summary>
    /// The time it takes for the hunger value to decay once
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan HungerDecayTime = TimeSpan.FromMinutes(14);

    /// <summary>
    /// The time when the hunger threshold will decay next.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextDecayTime;

    /// <summary>
    /// A dictionary relating hunger thresholds to corresponding alerts.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<HungerThreshold, ProtoId<AlertPrototype>> HungerThresholdAlerts = new()
    {
        { HungerThreshold.Okay, "HungerOkay" },
        { HungerThreshold.Peckish, "HungerPeckish" },
        { HungerThreshold.Hungry, "HungerHungry" },
        { HungerThreshold.Starving, "HungerStarving" },
    };

    /// <summary>
    /// A dictionary relating hunger thresholds to slowdown
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<HungerThreshold, float> HungerThresholdSlowdown = new()
    {
        { HungerThreshold.Okay, 1.0f },
        { HungerThreshold.Peckish, 1.0f },
        { HungerThreshold.Hungry, 0.85f },
        { HungerThreshold.Starving, 0.6f },
    };
}

[Serializable, NetSerializable]
public enum HungerThreshold
{
    Okay = 3,
    Peckish = 2,
    Hungry = 1,
    Starving = 0,
}
