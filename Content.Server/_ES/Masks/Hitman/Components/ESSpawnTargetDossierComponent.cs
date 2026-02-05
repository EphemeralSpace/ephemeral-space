using Content.Shared.Paper;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Hitman.Components;

[RegisterComponent]
[Access(typeof(ESSpawnTargetDossierSystem))]
public sealed partial class ESSpawnTargetDossierComponent : Component
{
    [DataField]
    public string ContainerId = "entity_storage";

    /// <summary>
    /// Prototype for the thing that will have the clues written on it.
    /// </summary>
    [DataField]
    public EntProtoId<PaperComponent> PaperPrototype = "Paper";

    /// <summary>
    /// Number of clues per dossier
    /// </summary>
    [DataField]
    public int ClueCount = 3;
}
