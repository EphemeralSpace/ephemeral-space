using System.Diagnostics;
using System.Linq;
using Content.Server._ES.Auditions.Components;
using Content.Server.Administration;
using Content.Server.Mind;
using Content.Shared._ES.Auditions;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Random.Helpers;
using JetBrains.Annotations;
using Robust.Shared.Random;
using Robust.Shared.Toolshed;

namespace Content.Server._ES.Auditions;

/// <summary>
/// This handles the server-side of auditioning!
/// </summary>
public sealed class ESAuditionsSystem : ESSharedAuditionsSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
    }

    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!_mind.TryGetMind(ev.Mob, out var mind, out _))
            return;

        var cast = EnsureComp<ESStationCastComponent>(ev.Station);
        cast.Crew.Add(mind);
    }

    public Entity<MindComponent, ESCharacterComponent> GetRandomCharacterFromPool(EntityUid station)
    {
        var producer = GetProducer();

        var cast = EnsureComp<ESStationCastComponent>(station);

        if (producer.UnusedCharacterPool.Count < producer.PoolRefreshSize)
        {
            Log.Debug($"Pool depleted below refresh size ({producer.PoolRefreshSize}). Replenishing pool.");
            GenerateCast(producer.PoolSize - producer.UnusedCharacterPool.Count, producer);
        }

        var weightedMembers = new Dictionary<EntityUid, float>();
        foreach (var castMember in producer.UnusedCharacterPool)
        {
            if (!TryComp<ESCharacterComponent>(castMember, out var characterComponent))
                continue;

            // arbitrary formula but good enough
            weightedMembers.Add(castMember, 4 * MathF.Pow(4, characterComponent.Relationships.Keys.Count(k => cast.Crew.Contains(k))));
        }

        var ent = _random.Pick(weightedMembers);
        producer.UnusedCharacterPool.Remove(ent);
        return (ent, Comp<MindComponent>(ent), Comp<ESCharacterComponent>(ent));
    }

    /// <summary>
    /// Hires a cast, and integrates relationships between all of the characters.
    /// </summary>
    public void GenerateCast(int count, ESProducerComponent? producer = null)
    {
        producer ??= GetProducer();

        var preEvt = new ESPreCastGenerateEvent(producer);
        RaiseLocalEvent(ref preEvt);

        var newCharacters = new List<EntityUid>();

        for (var i = 0; i < count; i++)
        {
            var newCrew = GenerateCharacter(producer: producer);
            newCharacters.Add(newCrew);
        }

        var psgEvt = new ESPostShipGenerateEvent(producer);
        RaiseLocalEvent(ref psgEvt);

        foreach (var group in producer.SocialGroups)
        {
            var comp = EnsureComp<ESSocialGroupComponent>(group);
            if (comp.Integrated)
                continue;

            var ent = (group, comp);

            var pre = new ESSocialGroupPreIntegrationEvent(ent);
            RaiseLocalEvent(ref pre);

            IntegrateRelationshipGroup(comp.RelativeContext, comp.Members);
            comp.Integrated = true;

            var post = new ESSocialGroupPostIntegrationEvent(ent);
            RaiseLocalEvent(ref post);
        }

        IntegrateRelationshipGroup(producer.IntercrewContext, newCharacters);

        var postEvt = new ESPostCastGenerateEvent(producer);
        RaiseLocalEvent(ref postEvt);
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Round)]
public sealed class CastCommand : ToolshedCommand
{
    private ESAuditionsSystem? _auditions;

    [CommandImplementation("generate")]
    public IEnumerable<string> Generate(int crewSize = 10)
    {
        _auditions ??= GetSys<ESAuditionsSystem>();

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        _auditions.GenerateCast(crewSize);

        yield return $"Generated cast in {stopwatch.Elapsed.TotalMilliseconds} ms.";
    }

    [CommandImplementation("view")]
    public IEnumerable<string> View([PipedArgument] EntityUid castMember)
    {
        _auditions ??= GetSys<ESAuditionsSystem>();
        if (!EntityManager.TryGetComponent<ESCharacterComponent>(castMember, out var character))
        {
            yield return "Invalid cast member object (did not have CharacterComponent)!";
        }
        else
        {
            yield return $"{character.Name}, {character.Profile.Age} years old ({character.DateOfBirth.ToShortDateString()})\nBackground: {character.Background}\nRelationships\n";
            Dictionary<string, List<EntityUid>> relationships = new();
            foreach (var relationship in character.Relationships)
            {
                if (relationships.ContainsKey(relationship.Value))
                    relationships[relationship.Value].Add(relationship.Key);
                else
                    relationships[relationship.Value] = [relationship.Key];
            }

            foreach (var relationship in relationships)
            {
                yield return $"{relationship.Key} ({relationship.Value.Count}): {string.Join(", ", relationship.Value.ToArray())}";
            }
        }
    }

    [CommandImplementation("viewAll")]
    public IEnumerable<string> ViewAll()
    {
        _auditions ??= GetSys<ESAuditionsSystem>();
        var producer = _auditions.GetProducer();
        foreach (var character in producer.Characters)
        {
            foreach (var line in View(character))
            {
                yield return line;
            }
        }
    }
}

/// <summary>
/// Fires prior to this social group's relationships being integrated.
/// </summary>
[ByRefEvent, PublicAPI]
public readonly record struct ESSocialGroupPreIntegrationEvent(Entity<ESSocialGroupComponent> Group);

/// <summary>
/// Fires after this social group's relationships have been integrated.
/// </summary>
[ByRefEvent, PublicAPI]
public readonly record struct ESSocialGroupPostIntegrationEvent(Entity<ESSocialGroupComponent> Group);

/// <summary>
/// Fires prior to any generation events (captain group, crew groups, etc).
/// </summary>
[ByRefEvent, PublicAPI]
public readonly record struct ESPreCastGenerateEvent(ESProducerComponent Producer);

/// <summary>
/// Fires after the primary generation events (captain group, crew group, etc) but before integration of relationships.
/// </summary>
[ByRefEvent, PublicAPI]
public readonly record struct ESPostShipGenerateEvent(ESProducerComponent Producer);

/// <summary>
/// Fires after all relationships have been integrated.
/// </summary>
[ByRefEvent, PublicAPI]
public readonly record struct ESPostCastGenerateEvent(ESProducerComponent Producer);
