using System.Diagnostics;
using System.Linq;
using Content.Server._ES.Auditions.Components;
using Content.Server.Administration;
using Content.Server.Mind;
using Content.Shared._ES.Auditions;
using Content.Shared._ES.Auditions.Components;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.Localizations;
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

    public EntityUid GetRandomCharacterFromPool(Entity<ESProducerComponent?> station)
    {
        if (!Resolve(station, ref station.Comp, false))
            return _mind.CreateMind(null);

        var cast = EnsureComp<ESStationCastComponent>(station);

        if (station.Comp.UnusedCharacterPool.Count < station.Comp.PoolRefreshSize)
        {
            Log.Debug($"Pool depleted below refresh size ({station.Comp.PoolRefreshSize}). Replenishing pool.");
            GenerateCast((station, station.Comp), station.Comp.PoolSize - station.Comp.UnusedCharacterPool.Count);
        }

        var weightedMembers = new Dictionary<EntityUid, float>();
        foreach (var castMember in station.Comp.UnusedCharacterPool)
        {
            if (!TryComp<ESCharacterComponent>(castMember, out var characterComponent))
                continue;

            // arbitrary formula but good enough
            weightedMembers.Add(castMember, 4 * MathF.Pow(4, characterComponent.Relationships.Keys.Count(k => cast.Crew.Contains(k))));
        }

        var ent = _random.Pick(weightedMembers);
        station.Comp.UnusedCharacterPool.Remove(ent);
        return ent;
    }

    /// <summary>
    /// Hires a cast, and integrates relationships between all of the characters.
    /// </summary>
    public void GenerateCast(Entity<ESProducerComponent> producer, int count)
    {
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

        foreach (var group in producer.Comp.SocialGroups)
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

        IntegrateRelationshipGroup(producer.Comp.IntercrewContext, newCharacters);

        var postEvt = new ESPostCastGenerateEvent(producer);
        RaiseLocalEvent(ref postEvt);
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Round)]
public sealed class CastCommand : ToolshedCommand
{
    private ESAuditionsSystem? _auditions;

    [CommandImplementation("generate")]
    public IEnumerable<string> Generate([PipedArgument] EntityUid station, int crewSize = 10)
    {
        if (!TryComp<ESProducerComponent>(station, out var producer))
            yield break;

        _auditions ??= GetSys<ESAuditionsSystem>();

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        _auditions.GenerateCast((station, producer), crewSize);

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
            yield return $"{character.Name}, {character.Profile.Age} years old ({character.DateOfBirth.ToShortDateString()})\nBackground: {character.Background}\nPersonality: {ContentLocalizationManager.FormatList(character.PersonalityTraits.Select(p => Loc.GetString(p)).ToList())}\nRelationships";
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
    public IEnumerable<string> ViewAll([PipedArgument] EntityUid station)
    {
        if (!TryComp<ESProducerComponent>(station, out var producer))
            yield break;

        _auditions ??= GetSys<ESAuditionsSystem>();
        foreach (var character in producer.Characters)
        {
            foreach (var line in View(character))
            {
                yield return line;
            }

            yield return string.Empty;
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
