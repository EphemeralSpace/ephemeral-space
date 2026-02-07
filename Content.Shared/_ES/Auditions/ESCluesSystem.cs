using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._ES.Auditions.Components;
using Content.Shared.Humanoid;
using Robust.Shared.ColorNaming;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// Clues are general character-related aspects that can be used to identify players.
/// They are based on a character's round-start attributes and may become out-of-date over a round.
/// </summary>
public sealed class ESCluesSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ESSharedAuditionsSystem _auditions = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearance = default!;

    public IEnumerable<string> GetSignificantInitialClues(Entity<ESCharacterComponent?> mind, int minFreq = 0)
    {
        if (!Resolve(mind, ref mind.Comp))
            return [];

        var initials = new HashSet<string>();
        foreach (var character in mind.Comp.BaseName.ToCharArray())
        {
            if (!char.IsAsciiLetterUpper(character))
                continue;
            var clueString = Loc.GetString("es-clue-initial-fmt", ("initial", character));
            if (minFreq > 0 && GetSignificantInitialFrequency(clueString) < minFreq)
                continue;
            initials.Add(clueString);
        }

        return initials;
    }

    public string GetEyeColorClue(Entity<ESCharacterComponent?> mind)
    {
        Resolve(mind, ref mind.Comp);
        var color = mind.Comp?.Profile.Appearance.EyeColor ?? Color.Black;
        return Loc.GetString("es-clue-eye-fmt", ("color", ColorNaming.Describe(color, Loc)));
    }

    public string GetHairColorClue(Entity<ESCharacterComponent?> mind)
    {
        if (!Resolve(mind, ref mind.Comp))
            return string.Empty;

        return Loc.GetString("es-clue-hair-fmt", ("color", GetHairColorString(mind.Comp.Profile.Appearance.HairColor)));
    }

    /// <summary>
    /// Gives a descriptor of a given hair color.
    /// If it's a preset, it'll use the proper name (blond, auburn, etc.)
    /// If not, it'll use a generic fallback.
    /// </summary>
    public string GetHairColorString(Color color)
    {
        if (TryGetHairColorGroupFromColor(color, out var hairColor))
            return Loc.GetString(hairColor.Name);
        return ColorNaming.Describe(color, Loc);
    }

    /// <summary>
    /// Tries to get the "type" of hair color based on a color.
    /// Fails if it's not a preset hair color.
    /// </summary>
    public bool TryGetHairColorGroupFromColor(Color color, [NotNullWhen(true)] out ESHairColorPrototype? colorGroup)
    {
        colorGroup = null;
        foreach (var prototype in _prototype.EnumeratePrototypes<ESHairColorPrototype>())
        {
            if (prototype.Abstract)
                continue;

            if (!prototype.Colors.Contains(color))
                continue;

            colorGroup = prototype;
            return true;
        }

        return false;
    }

    public string GetAgeClue(Entity<ESCharacterComponent?> mind)
    {
        if (!Resolve(mind, ref mind.Comp))
            return string.Empty;

        return _humanoidAppearance.GetAgeRepresentation(mind.Comp.Profile.Species, mind.Comp.Profile.Age);
    }

    public string GetSexClue(Entity<ESCharacterComponent?> mind)
    {
        if (!Resolve(mind, ref mind.Comp))
            return string.Empty;

        return SexToString(mind.Comp.Profile.Sex);
    }

    public string SexToString(Sex sex)
    {
        return sex switch
        {
            Sex.Male => Loc.GetString("es-clue-sex-male"),
            Sex.Female => Loc.GetString("es-clue-sex-female"),
            _ => Loc.GetString("es-clue-sex-nb"),
        };
    }

    public IEnumerable<string> GetClues(Entity<ESCharacterComponent?> mind, int count, int minFreq = 3)
    {
        if (!Resolve(mind, ref mind.Comp))
            yield break;

        var clueOptions = new List<ESClue>();
        foreach (var clue in Enum.GetValues<ESClue>())
        {
            // For initials, check there is at least one valid option
            if (clue == ESClue.Initial)
            {
                var validInitials = GetSignificantInitialClues(mind, minFreq);
                if (validInitials.Any())
                    clueOptions.Add(ESClue.Initial);
                continue;
            }

            if (GetClueFrequency(mind, clue) < minFreq)
                continue;
            clueOptions.Add(clue);
        }

        var clues = _random.GetItems(clueOptions, Math.Min(clueOptions.Count, count), allowDuplicates: false);
        foreach (var clue in clues)
        {
            // Special-case for initials
            if (clue == ESClue.Initial)
            {
                yield return _random.Pick(GetSignificantInitialClues(mind, minFreq).ToList());
            }
            else
            {
                yield return GetClue(mind, clue);
            }
        }
    }

    public string GetClue(Entity<ESCharacterComponent?> mind, ESClue clue)
    {
        return clue switch
        {
            ESClue.Initial => _random.Pick(GetSignificantInitialClues(mind).ToList()),
            ESClue.HairColor => GetHairColorClue(mind),
            ESClue.EyeColor => GetEyeColorClue(mind),
            ESClue.Age => GetAgeClue(mind),
            ESClue.Sex => GetSexClue(mind),
            _ => throw new ArgumentOutOfRangeException(nameof(clue), clue, null)
        };
    }

    /// <summary>
    /// Returns the number of times a given clue occurs among the players in the game.
    /// </summary>
    public int GetClueFrequency(Entity<ESCharacterComponent?> mind, ESClue clue)
    {
        var clueValue = GetClue(mind, clue);

        var frequency = 0;
        foreach (var character in _auditions.GetCharacters())
        {
            if (GetClue(character.AsNullable(), clue) == clueValue)
                frequency++;
        }
        return frequency;
    }

    /// <summary>
    /// Returns the number of times a given initial occurs among the players in the game.
    /// </summary>
    public int GetSignificantInitialFrequency(string initialClue)
    {
        var frequency = 0;
        foreach (var character in _auditions.GetCharacters())
        {
            if (GetSignificantInitialClues(character.AsNullable()).Contains(initialClue))
                frequency++;
        }
        return frequency;
    }
}

public enum ESClue : byte
{
    Initial,
    HairColor,
    EyeColor,
    Age,
    Sex,
}
