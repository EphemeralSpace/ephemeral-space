using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
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
    /// </summary>
    [DataField]
    public FixedPoint2 Threshold = 100;

    /// <summary>
    /// Sound optionally played when this object is broken
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = new SoundCollectionSpecifier("MetalBreak");
}

[Serializable, NetSerializable]
public enum ESBreakableVisuals : byte
{
    Broken,
}
