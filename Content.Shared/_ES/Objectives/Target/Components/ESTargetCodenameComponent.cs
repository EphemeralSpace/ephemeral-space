using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Objectives.Target.Components;

/// <summary>
/// Gives an objective a title referring to a codename instead of telling you who it is.
/// </summary>
[RegisterComponent]
[Access(typeof(ESTargetCodenameSystem))]
public sealed partial class ESTargetCodenameComponent : Component
{
    /// <summary>
    /// Dataset used for the arbitrary codenames
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> CodenameDataset = "ESInsiderCodenames";

    /// <summary>
    /// Locale string for the objective title. Passes in <see cref="Codename"/> as "codename"
    /// </summary>
    [DataField]
    public LocId? Title;

    /// <summary>
    /// The target's codename.
    /// </summary>
    [DataField]
    public LocId? Codename;
}
