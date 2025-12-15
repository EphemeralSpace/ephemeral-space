using Content.Shared._ES.Objectives.Components;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Objectives.Components;

/// <summary>
/// Objective used with <see cref="ESCounterObjectiveComponent"/> for a cumulative amount of damage from specific types.
/// </summary>
[RegisterComponent]
[Access(typeof(ESTakeDamageObjectiveSystem))]
public sealed partial class ESTakeDamageObjectiveComponent : Component
{
    /// <summary>
    ///     Different types of damages one can roll
    /// </summary>
    [DataField]
    public List<ProtoId<DamageTypePrototype>> RequiredDamages;

    /// <summary>
    ///     The damage type selected
    /// </summary>
    [DataField]
    public ProtoId<DamageTypePrototype>? SelectedDamage;

    /// <summary>
    ///     The description for this objective, where $damagesource will become the source
    /// </summary>
    [DataField(required: true)]
    public LocId DescriptionLoc { get; private set; }

    /// <summary>
    ///     The description for this objective, where $damagesource will become the source
    /// </summary>
    [DataField(required: true)]
    public LocId NameLoc { get; private set; }
}
