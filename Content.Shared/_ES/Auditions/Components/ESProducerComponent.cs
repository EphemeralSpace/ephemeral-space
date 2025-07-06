using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Auditions.Components;

/// <summary>
/// This is the cast component placed onto the producer entity.
/// </summary>
[RegisterComponent]
public sealed partial class ESProducerComponent : Component
{
    /// <summary>
    /// The amount of characters we refresh <see cref="UnusedCharacterPool"/> to.
    /// </summary>
    [DataField]
    public int PoolSize = 50;

    /// <summary>
    /// Once the pool goes below this amount, we'll refresh it
    /// </summary>
    [DataField]
    public int PoolRefreshSize = 25;

    /// <summary>
    /// All the characters in the cast.
    /// </summary>
    [DataField]
    public List<EntityUid> Characters = new();

    /// <summary>
    /// A pool of characters who have not been taken by players.
    /// </summary>
    [DataField]
    public List<EntityUid> UnusedCharacterPool = new();

    /// <summary>
    /// List of all active social groups.
    /// </summary>
    [DataField]
    public List<EntityUid> SocialGroups = new ();

    [DataField]
    public ESRelationshipContext IntercrewContext = new ("RelationshipPoolIntercrew", 0.25f);
}

/// <summary>
/// Configuration for integrating relationships.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public partial struct ESRelationshipContext
{
    /// <summary>
    /// List of possible relationships.
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomPrototype> PoolPrototype;

    /// <summary>
    /// How likely is a relationship to spark in this context?
    /// </summary>
    [DataField]
    public float RelationshipProbability = 0.5f;

    /// <summary>
    /// How likely is a relationship to be mutual (both sides have the same relationship)?
    /// </summary>
    [DataField]
    public float UnificationProbability = 1f;

    /// <summary>
    /// If the relationship isnt mutual, what are other possible relationships to give? If null, no relationship is assigned.
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomPrototype>? SeperatePoolPrototype;

    public ESRelationshipContext()
    {
    }

    public ESRelationshipContext(string prototype, float probability)
    {
        RelationshipProbability = probability;
        PoolPrototype = prototype;
        UnificationProbability = 1f;
        SeperatePoolPrototype = null;
    }

    public ESRelationshipContext(string prototype, float probability, float unificationProbability, string? seperatePrototype)
    {
        RelationshipProbability = probability;
        PoolPrototype = prototype;
        UnificationProbability = unificationProbability;
        SeperatePoolPrototype = seperatePrototype;
    }
}
