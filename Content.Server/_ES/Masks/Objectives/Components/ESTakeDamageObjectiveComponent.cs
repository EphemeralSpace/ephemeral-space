using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Objectives.Components;

[RegisterComponent]
[Access(typeof(ESTakeDamageObjectiveSystem))]
public sealed partial class ESTakeDamageObjectiveComponent : Component
{
    /// <summary>
    ///     Diffrent types of damages one can roll
    /// </summary>
    [DataField]
    public List<ProtoId<DamageGroupPrototype>> RequiredDamages;

    /// <summary>
    ///     The damage type selected
    /// </summary>
    [DataField]
    public DamageGroupPrototype? SelectedDamage;

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
