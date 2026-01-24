using System.Diagnostics;
using System.Linq;
using Content.Server._ES.Auditions.Components;
using Content.Server.Administration;
using Content.Server.Mind;
using Content.Shared._ES.Auditions;
using Content.Shared._ES.Auditions.Components;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
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

        if (station.Comp.UnusedCharacterPool.Count < station.Comp.PoolRefreshSize)
        {
            Log.Debug($"Pool depleted below refresh size ({station.Comp.PoolRefreshSize}). Replenishing pool.");
            GenerateCast((station, station.Comp), station.Comp.PoolSize - station.Comp.UnusedCharacterPool.Count);
        }

        if (station.Comp.UnusedCharacterPool.Count == 0)
            throw new Exception("Failed to replenish character pool!");

        return _random.PickAndTake(station.Comp.UnusedCharacterPool);
    }

    /// <summary>
    /// Hires a cast, and integrates relationships between all of the characters.
    /// </summary>
    public void GenerateCast(Entity<ESProducerComponent> producer, int count)
    {
        for (var i = 0; i < count; i++)
        {
            GenerateCharacter(producer: producer);
        }
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Round)]
public sealed class CastCommand : ToolshedCommand
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private ESAuditionsSystem? _auditions;
    private ESCluesSystem? _clues;

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
        _clues ??= GetSys<ESCluesSystem>();
        if (!EntityManager.TryGetComponent<ESCharacterComponent>(castMember, out var character))
        {
            throw new Exception($"Entity {castMember} did not have character component!");
        }

        var gender = Loc.GetString($"humanoid-profile-editor-pronouns-{character.Profile.Gender.ToString().ToLower()}-text");
        yield return
            $"{character.Name} ({gender}), {character.Profile.Age} years old ({character.DateOfBirth.ToShortDateString()})\n" +
            $"\t{string.Join(", ", _clues.GetSignificantInitialClues(castMember).Select(c => $"{c} (count: {_clues.GetSignificantInitialFrequency(c)})"))}\n" +
            $"\t{_clues.GetSexClue(castMember)} (count: {_clues.GetClueFrequency(castMember, ESClue.Sex)})\n" +
            $"\t{_clues.GetAgeClue(castMember)} (count: {_clues.GetClueFrequency(castMember, ESClue.Age)})\n" +
            $"\t{_clues.GetEyeColorClue(castMember)} (count: {_clues.GetClueFrequency(castMember, ESClue.EyeColor)})\n" +
            $"\t{_clues.GetHairColorClue(castMember)} (count: {_clues.GetClueFrequency(castMember, ESClue.HairColor)})";
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

    [CommandImplementation("viewPresent")]
    public IEnumerable<string> ViewPresent([PipedArgument] EntityUid station)
    {
        if (!TryComp<ESProducerComponent>(station, out var producer))
            yield break;

        _auditions ??= GetSys<ESAuditionsSystem>();
        foreach (var character in producer.UsedCharacters)
        {
            foreach (var line in View(character))
            {
                yield return line;
            }

            yield return string.Empty;
        }
    }

    [CommandImplementation("generateNames")]
    public IEnumerable<string> GenerateNames(int count)
    {
        _auditions ??= GetSys<ESAuditionsSystem>();

        for (var i = 0; i < count; i++)
        {
            var profile = HumanoidCharacterProfile.RandomWithSpecies();
            var species = _prototype.Index(profile.Species);

            _auditions.GenerateName(profile, species);
            yield return profile.Name;
        }
    }
}
