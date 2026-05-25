using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Breakable.Components;

/// <summary>
/// Used as a general source of state management for a breakable structure/object.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESBreakableSystem))]
public sealed partial class ESBreakableComponent : Component
{
    /// <summary>
    /// State of the entity. Either broken or operational
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Broken;

    /// <summary>
    /// Damage threshold which this object becomes broken at.
    /// If null, will be ignored.
    /// </summary>
    [DataField]
    public FixedPoint2? Threshold;
}

[Serializable, NetSerializable]
public enum ESBreakableVisuals : byte
{
    Broken,
}
