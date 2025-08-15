using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.CCVar;
using Content.Shared.Mind;
using Content.Shared.Random.Helpers;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// The main system for handling the creation, integration of relations
/// </summary>
public abstract partial class ESSharedAuditionsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public bool RandomCharactersEnabled;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_config, ESCVars.ESRandomCharacters, val => RandomCharactersEnabled = val, true);
    }

    /// <summary>
    /// Changes the relationship between two characters. If the relationship is not mutual, then it assigns A's relationship with B, and does not affect B.
    /// </summary>
    public void ChangeRelationship(Entity<ESCharacterComponent> characterA, Entity<ESCharacterComponent> characterB, string relationshipId, bool mutual = true)
    {
        characterA.Comp.Relationships[characterB] = relationshipId;
        if (mutual)
            characterB.Comp.Relationships[characterA] = relationshipId;
        Dirty(characterA);
    }

    /// <summary>
    /// Removes the relationship between two characters. If the removal is not mutual, then it removes A's relationship with B, and does not affect B.
    /// </summary>
    public void RemoveRelationship(Entity<ESCharacterComponent> characterA, Entity<ESCharacterComponent> characterB, bool mutual = true)
    {
        characterA.Comp.Relationships.Remove(characterB);
        if (mutual)
            characterB.Comp.Relationships.Remove(characterA);
        Dirty(characterA);
    }

    /// <summary>
    /// Attempts to integrate a relationship between two characcters with the following relationship context.
    /// </summary>
    public void IntegrateRelationship(ESRelationshipContext context, Entity<ESCharacterComponent> characterA, Entity<ESCharacterComponent> characterB)
    {
        if (!_random.Prob(context.RelationshipProbability))
            return;

        var weightList = _prototypeManager.Index(context.PoolPrototype);
        var relationship =  weightList.Pick(_random);
        ChangeRelationship(characterA, characterB, relationship);

        var mutual = _random.Prob(context.UnificationProbability);
        if (mutual)
            return;

        if (context.SeperatePoolPrototype is null)
        {
            RemoveRelationship(characterA, characterB, false);
            return;
        }
        var seperateWeightList = _prototypeManager.Index(context.SeperatePoolPrototype.Value);
        var newRelationship =  seperateWeightList.Pick(_random);

        ChangeRelationship(characterA, characterB, newRelationship, false);
    }

    /// <summary>
    /// Attempts to integrate relationships with a group of characters. Uses a list of Entity&lt;CharacterComponent&gt;s.
    /// </summary>
    public void IntegrateRelationshipGroup(ESRelationshipContext context, List<Entity<ESCharacterComponent>> characters)
    {
        // rain here. im no profesional, but i pulled out the paper & pencil for this "algorithm".
        // that goes to show how really great i am with programming things. yay.
        var stopwatch = new Stopwatch();

        var debugCalls = 0;
        stopwatch.Start();

        for (var i = 0; i < characters.Count; i++)
        {
            for (var j = 0; j < i; j++)
            {
                IntegrateRelationship(context, characters[i], characters[j]);
                debugCalls++;
            }
        }

        Log.Info($"Called {debugCalls} times in {stopwatch.Elapsed}.");
    }

    /// <summary>
    /// Attempts to integrate relationships with a group of characters. Uses a list of EntityUids.
    /// </summary>
    public void IntegrateRelationshipGroup(ESRelationshipContext context, List<EntityUid> characters)
    {
        var stopwatch = new Stopwatch();

        var debugCalls = 0;
        stopwatch.Start();

        for (var i = 0; i < characters.Count; i++)
        {
            for (var j = 0; j < i; j++)
            {
                IntegrateRelationship(context,
                    (characters[i], EnsureComp<ESCharacterComponent>(characters[i])),
                    (characters[j], EnsureComp<ESCharacterComponent>(characters[j])));
                debugCalls++;
            }
        }

        Log.Info($"Called {debugCalls} times in {stopwatch.Elapsed}.");
    }

    public void IntegrateRelationshipGroup(ESSocialGroupComponent groupComponent)
    {
        IntegrateRelationshipGroup(groupComponent.RelativeContext, groupComponent.Members);
    }

    /// <summary>
    /// Generates a completely empty crew entity.
    /// </summary>
    public Entity<ESSocialGroupComponent> GenerateEmptySocialGroup(Entity<ESProducerComponent> producer)
    {
        var newCrew = EntityManager.Spawn();
        var component = EnsureComp<ESSocialGroupComponent>(newCrew);
        producer.Comp.SocialGroups.Add(newCrew);

        return (newCrew, component);
    }
}
