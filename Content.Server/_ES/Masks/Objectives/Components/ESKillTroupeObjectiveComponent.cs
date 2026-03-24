using Content.Shared._ES.Masks;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Objectives.Components;

/// <summary>
///     Objective for killing a member of a given troupe.
///     The target kill count is set by <see cref="NumberObjectiveComponent"/>.
/// </summary>
/// <seealso cref="ESKillTroupeObjectiveSystem"/>
[RegisterComponent]
[Access(typeof(ESKillTroupeObjectiveSystem))]
public sealed partial class ESKillTroupeObjectiveComponent : Component
{
    /// <summary>
    ///     The troupe the victim must be a part of
    /// </summary>
    [DataField]
    public ProtoId<ESTroupePrototype> Troupe;

    /// <summary>
    ///     If true, kills will count if the victim is NOT part of <see cref="Troupe"/>
    /// </summary>
    [DataField]
    public bool Invert;
}
