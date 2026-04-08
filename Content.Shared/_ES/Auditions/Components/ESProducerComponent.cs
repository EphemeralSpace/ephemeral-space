using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Auditions.Components;

/// <summary>
/// This is the cast component placed onto the producer entity.
/// </summary>
[RegisterComponent]
public sealed partial class ESProducerComponent : Component
{
    /// <summary>
    /// All the characters in the cast.
    /// </summary>
    [DataField]
    public List<EntityUid> Characters = new();

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> OpinionDataset = "ESCharacterOpinionConcepts";

    [DataField]
    public int OpinionConceptCount = 20;

    /// <summary>
    /// A random pool of concepts that generated characters can have opinions on.
    /// This is a pool so that players will often have opinions on similar things.
    /// </summary>
    [DataField]
    public List<LocId> OpinionConcepts = new();
}
