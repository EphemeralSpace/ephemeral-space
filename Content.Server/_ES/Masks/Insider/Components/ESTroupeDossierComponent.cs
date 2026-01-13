using Content.Shared.Dataset;
using Content.Shared.Paper;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Insider.Components;

[RegisterComponent]
[Access(typeof(ESTroupeDossierSystem))]
public sealed partial class ESTroupeDossierComponent : Component
{
    /// <summary>
    /// Container where the <see cref="PaperPrototype"/> is spawned.
    /// </summary>
    [DataField]
    public string ContainerId = StorageComponent.ContainerId;

    /// <summary>
    /// Prototype for the thing that will have the clues written on it.
    /// </summary>
    [DataField]
    public EntProtoId<PaperComponent> PaperPrototype = "Paper";

    /// <summary>
    /// Number of crew dossiers to spawn
    /// </summary>
    [DataField]
    public int CrewCount = 2;

    /// <summary>
    /// Number of non-crew dossiers to spawn
    /// </summary>
    [DataField]
    public int NonCrewCount = 1;

    /// <summary>
    /// Number of clues per dossier
    /// </summary>
    [DataField]
    public int ClueCount = 3;

    /// <summary>
    /// Dataset used for the arbitrary codenames
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> CodenameDataset = "Adjectives";
}
