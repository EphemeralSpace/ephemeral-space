using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Objectives.Components;

/// <summary>
///     Handles the Sacrifice's objective and behavior of healing others that die near them.
///     The behavior is also tied to the objective--not just the completion logic, so
///     anyone with this objective will have the effect of the Sacrifice.
/// </summary>
/// <seealso cref="ESSacrificeHealObjectiveSystem"/>
[RegisterComponent]
public sealed partial class ESSacrificeHealObjectiveComponent : Component
{
    /// <summary>
    ///     Popup message to be shown to players healed by the sacrifice's death.
    /// </summary>
    [DataField]
    public LocId SacrificePopupMessage = "es-sacrifice-popup-heroic-sacrifice";

    /// <summary>
    ///     Radius to apply effect in.
    /// </summary>
    [DataField]
    public float Radius = 4f;

    /// <summary>
    ///     The reagent(s) to be added to mobs in radius on death.
    /// </summary>
    [DataField(required: true)]
    public Solution SacrificeSolution = default!;
}
