using System.Diagnostics;
using System.Linq;
using Content.Server.Administration;
using Content.Shared._ES.Auditions;
using Content.Shared._ES.Auditions.Components;
using Content.Shared.Administration;
using Content.Shared.Localizations;
using Robust.Shared.Enums;
using Robust.Shared.Random;
using Robust.Shared.Toolshed;

namespace Content.Server._ES.Auditions;

/// <summary>
/// This handles the server-side of auditioning!
/// </summary>
public sealed class ESAuditionsSystem : ESSharedAuditionsSystem;

[ToolshedCommand, AdminCommand(AdminFlags.Round)]
public sealed partial class CastCommand : ToolshedCommand
{
    [Dependency] private IRobustRandom _random = default!;

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

        for (var i = 0; i < crewSize; ++i)
        {
            _auditions.GenerateCharacter((station, producer));
        }

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
            $"\t{_auditions.GetCharacterPrompt((castMember, character))}\n" +
            $"\tLikes: {ContentLocalizationManager.FormatList(character.Likes.Select(e => Loc.GetString(e)).ToList())}\n" +
            $"\tDislikes: {ContentLocalizationManager.FormatList(character.Dislikes.Select(e => Loc.GetString(e)).ToList())}\n" +
            $"\t{string.Join(", ", _clues.GetSignificantInitialClues(castMember).Select(c => $"{c} ({_clues.GetSignificantInitialFrequency(c)})"))}\n" +
            $"\t{_clues.GetSexClue(castMember)} ({_clues.GetClueFrequency(castMember, ESClue.Sex)})\n" +
            $"\t{_clues.GetAgeClue(castMember)} ({_clues.GetClueFrequency(castMember, ESClue.Age)})\n" +
            $"\t{_clues.GetEyeColorClue(castMember)} ({_clues.GetClueFrequency(castMember, ESClue.EyeColor)})\n" +
            $"\t{_clues.GetHairColorClue(castMember)} ({_clues.GetClueFrequency(castMember, ESClue.HairColor)})";
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
        foreach (var character in producer.Characters)
        {
            foreach (var line in View(character))
            {
                yield return line;
            }

            yield return string.Empty;
        }
    }

    private static readonly List<Gender> Genders = [Gender.Male, Gender.Female, Gender.Epicene];

    [CommandImplementation("generateNames")]
    public IEnumerable<string> GenerateNames(int count)
    {
        _auditions ??= GetSys<ESAuditionsSystem>();

        for (var i = 0; i < count; i++)
        {
            yield return _auditions.GenerateName(ESNameConfig.Default, _random.Pick(Genders), out _);
        }
    }
}
