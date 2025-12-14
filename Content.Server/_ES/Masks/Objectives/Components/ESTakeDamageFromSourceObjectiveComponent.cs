using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Objectives.Components;

[RegisterComponent]
[Access(typeof(ESTakeDamageFromSourceObjectiveSystem))]
public sealed partial class ESTakeDamageFromSourceObjectiveComponent : Component
{
    /// <summary>
    ///     Diffrent types of damages one can roll
    /// </summary>
    [DataField]
    public List<ProtoId<EntityPrototype>> PossibleSources = new();

    /// <summary>
    ///     The source selected
    /// </summary>
    [DataField]
    public EntityPrototype? SelectedSource;

    /// <summary>
    ///     The total type of damage objective haver has taken
    /// </summary>
    [DataField]
    public float TotalDamage = 0;

    /// <summary>
    ///     The description for this objective, where $damagetype will become the damage type
    /// </summary>
    [DataField(required: true)]
    public LocId DescriptionLoc { get; private set; }
}
