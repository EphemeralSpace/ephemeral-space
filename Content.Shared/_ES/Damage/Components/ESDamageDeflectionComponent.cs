using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Damage.Components;

/// <summary>
/// Used for a component which deflects damage dealt to an entity which is under a certain threshold.
/// This is distinct from armor, which *reduces* damage. Damage dealt to an entity with this component
/// is always preserved, but is just conditionally ignored based on total damage.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESDamageDeflectionSystem))]
public sealed partial class ESDamageDeflectionComponent : Component
{
    /// <summary>
    /// Damage dealt underneath this threshold will be ignored entirely
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 Threshold;
}
