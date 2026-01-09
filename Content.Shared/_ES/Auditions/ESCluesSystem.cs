using System.Linq;
using Content.Shared._ES.Auditions.Components;
using Content.Shared.Humanoid;
using Robust.Shared.Collections;
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
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearance = default!;

    public string GetSignificantInitialClue(Entity<ESCharacterComponent?> mind)
    {
        if (!Resolve(mind, ref mind.Comp))
            return "?";

        var candidates = new ValueList<char>();
        foreach (var character in mind.Comp.BaseName.ToCharArray())
        {
            if (char.IsAsciiLetterUpper(character))
                candidates.Add(character);
        }

        var initial = candidates.Any() ? $"{_random.Pick(candidates)}" : "?";
        return Loc.GetString("es-clue-initial-fmt", ("initial", initial));
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

        var colorString = ColorNaming.Describe(mind.Comp.Profile.Appearance.HairColor, Loc);
        if (_prototype.TryIndex(mind.Comp.Profile.Appearance.HairColorGroup, out var hairColor))
            colorString = Loc.GetString(hairColor.Name);

        return Loc.GetString("es-clue-hair-fmt", ("color", colorString));
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

        return mind.Comp.Profile.Sex switch
        {
            Sex.Male => Loc.GetString("es-clue-sex-male"),
            Sex.Female => Loc.GetString("es-clue-sex-female"),
            _ => Loc.GetString("es-clue-sex-nb"),
        };
    }

    public string GetZodiacClue(Entity<ESCharacterComponent?> mind)
    {
        if (!Resolve(mind, ref mind.Comp))
            return string.Empty;

        var day = mind.Comp.DateOfBirth.Day;
        var month = mind.Comp.DateOfBirth.Month;

        var sign = "generic-unknown-title";
        if (month == 1 && day >= 20 || month == 2 && day <= 18)
            sign = "es-clue-zodiac-aquarius";
        if (month == 2 && day >= 19 || month == 3 && day <= 20)
            sign = "es-clue-zodiac-pisces";
        if (month == 3 && day >= 21 || month == 4 && day <= 19)
            sign = "es-clue-zodiac-aries";
        if (month == 4 && day >= 20 || month == 5 && day <= 20)
            sign = "es-clue-zodiac-taurus";
        if (month == 5 && day >= 21 || month == 6 && day <= 20)
            sign = "es-clue-zodiac-gemini";
        if (month == 6 && day >= 21 || month == 7 && day <= 22)
            sign = "es-clue-zodiac-cancer";
        if (month == 7 && day >= 23 || month == 8 && day <= 22)
            sign = "es-clue-zodiac-leo";
        if (month == 8 && day >= 23 || month == 9 && day <= 22)
            sign = "es-clue-zodiac-virgo";
        if (month == 9 && day >= 23 || month == 10 && day <= 22)
            sign = "es-clue-zodiac-libra";
        if (month == 10 && day >= 23 || month == 11 && day <= 21)
            sign = "es-clue-zodiac-scorpio";
        if (month == 11 && day >= 22 || month == 12 && day <= 21)
            sign = "es-clue-zodiac-sagittarius";
        if (month == 12 && day >= 22 || month == 1 && day <= 19)
            sign = "es-clue-zodiac-capricorn";

        return Loc.GetString(sign);
    }
}
