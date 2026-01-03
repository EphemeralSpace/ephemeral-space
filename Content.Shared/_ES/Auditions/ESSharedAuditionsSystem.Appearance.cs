using System.Linq;
using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.CCVar;
using Content.Shared._ES.Random;
using Content.Shared.Dataset;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// The main system for handling the creation, integration of relations
/// </summary>
public abstract partial class ESSharedAuditionsSystem
{
    // TODO: re-examine when we get species.
    /// <remarks>
    /// I stole this list of hair colors off a wiki for some random MMO.
    /// </remarks>
    public static readonly IReadOnlyList<Color> RealisticHairColors = new List<Color>
    {
        Color.FromHex("#1c1f21"),
        Color.FromHex("#272a2c"),
        Color.FromHex("#312e2c"),
        Color.FromHex("#35261c"),
        Color.FromHex("#4b321f"),
        Color.FromHex("#5c3b24"),
        Color.FromHex("#6d4c35"),
        Color.FromHex("#6b503b"),
        Color.FromHex("#765c45"),
        Color.FromHex("#7f684e"),
        Color.FromHex("#99815d"),
        Color.FromHex("#a79369"),
        Color.FromHex("#af9c70"),
        Color.FromHex("#bba063"),
        Color.FromHex("#d6b97b"),
        Color.FromHex("#dac38e"),
        Color.FromHex("#9f7f59"),
        Color.FromHex("#845039"),
        Color.FromHex("#682b1f"),
        Color.FromHex("#7c140f"),
        Color.FromHex("#b64b28"),
        Color.FromHex("#a2502f"),
        Color.FromHex("#aa4e2b"),
        Color.FromHex("#1f1814"),
        Color.FromHex("#291f19"),
        Color.FromHex("#2e221b"),
        Color.FromHex("#37291e"),
        Color.FromHex("#2e2218"),
        Color.FromHex("#231b15"),
        Color.FromHex("#020202"),
        Color.FromHex("#9d7a50"),
    };

    public static readonly IReadOnlyList<Color> RealisticAgedHairColors = new List<Color>
    {
        Color.FromHex("#626262"),
        Color.FromHex("#808080"),
        Color.FromHex("#aaaaaa"),
        Color.FromHex("#c5c5c5"),
        Color.FromHex("#706c66"),
        Color.FromHex("#1c1f21"),
        Color.FromHex("#272a2c"),
        Color.FromHex("#312e2c"),
        Color.FromHex("#1f1814"),
        Color.FromHex("#291f19"),
        Color.FromHex("#231b15"),
        Color.FromHex("#020202"),
    };

    /// <summary>
    /// Eye colors, selected for variance and contrast with human skin tones
    /// </summary>
    public static readonly IReadOnlyList<Color> EyeColors =
    [
        Color.Black,
        Color.Gray,
        Color.MediumPurple,
        Color.Violet,
        Color.Azure,
        Color.ForestGreen,
        Color.LimeGreen,
        Color.DarkOrange,
        Color.IndianRed,
        Color.DarkKhaki,
        Color.FromHex("#3b1d0d"),
        Color.FromHex("#2a1100"),
    ];

    public const float CrazyHairChance = 0.10f;

    public const float ShavenChance = 0.55f;

    public const float YoungWeight = 5;
    public const float MiddleAgeWeight = 4;
    public const float OldAgeWeight = 1;

    private static readonly ProtoId<LocalizedDatasetPrototype> TendencyDataset = "ESPersonalityTendency";
    private static readonly ProtoId<LocalizedDatasetPrototype> TemperamentDataset = "ESPersonalityTemperament";

    /// <summary>
    /// Generates a character with randomized name, age, gender and appearance.
    /// </summary>
    public Entity<MindComponent, ESCharacterComponent> GenerateCharacter(Entity<ESProducerComponent> producer, [ForbidLiteral] string randomPrototype = "DefaultBackground")
    {
        var profile = RandomProfile(_random);
        var species = _prototypeManager.Index(profile.Species);

        GenerateName(profile, species);

        var (ent, mind) = _mind.CreateMind(null, profile.Name);
        var character = EnsureComp<ESCharacterComponent>(ent);

        var year = _config.GetCVar(ESCVars.ESInGameYear) - profile.Age;
        var month = _random.Next(1, 12);
        var day = _random.Next(1, DateTime.DaysInMonth(year, month));
        character.DateOfBirth = new DateTime(year, month, day);
        character.Background = _prototypeManager.Index<WeightedRandomPrototype>(randomPrototype).Pick(_random);
        character.Profile = profile;

        character.PersonalityTraits.Add(_random.Pick(_prototypeManager.Index(TendencyDataset)));
        character.PersonalityTraits.Add(_random.Pick(_prototypeManager.Index(TemperamentDataset)));

        character.Station = producer;

        Dirty(ent, character);

        producer.Comp.Characters.Add(ent);
        producer.Comp.UnusedCharacterPool.Add(ent);

        return (ent, mind, character);
    }

    public HumanoidCharacterProfile RandomProfile(IRobustRandom random, ProtoId<SpeciesPrototype>? speciesId = null)
    {
        speciesId ??= SharedHumanoidAppearanceSystem.DefaultSpecies;

        var species = _prototypeManager.Index(speciesId);

        var sex = random.Pick(species.Sexes);
        var gender = sex switch
        {
            Sex.Male => Gender.Male,
            Sex.Female => Gender.Female,
            _ => Gender.Epicene,
        };

        var profile = HumanoidCharacterProfile.DefaultWithSpecies(speciesId).WithSex(sex).WithGender(gender);

        var strategy = _prototypeManager.Index(species.SkinColoration).Strategy;
        profile.Appearance.SkinColor = strategy.InputType switch
        {
            SkinColorationStrategyInput.Unary => strategy.FromUnary(random.NextFloat(0f, 100f)),
            _ => strategy.ClosestSkinColor(_random.NextColor()),
        };

        profile.Age = random.Pick(new Dictionary<int, float>
        {
            { random.Next(species.MinAge, species.YoungAge), YoungWeight }, // Young age
            { random.Next(species.YoungAge, species.OldAge), MiddleAgeWeight }, // Middle age
            { random.Next(species.OldAge, species.MaxAge), OldAgeWeight }, // Old age
        });

        IReadOnlyList<Color> hairColors;
        if (profile.Age >= species.OldAge)
            hairColors = RealisticAgedHairColors;
        else if (profile.Age <= species.YoungAge)
            hairColors = RealisticHairColors;
        else
            hairColors = RealisticHairColors.Union(RealisticAgedHairColors).ToList();

        var hairColor = random.Prob(CrazyHairChance) ? random.NextColor() : random.Pick(hairColors);
        profile.Appearance.HairColor = hairColor;
        profile.Appearance.FacialHairColor = hairColor;

        profile.Appearance.EyeColor = random.Pick(EyeColors);

        List<ProtoId<MarkingPrototype>> hairOptions;
        if (random.Prob(CrazyHairChance))
        {
            hairOptions = species.UnisexHair.Union(species.FemaleHair).Union(species.MaleHair).ToList();
        }
        else
        {
            hairOptions = species.UnisexHair.Union(profile.Gender switch
            {
                Gender.Male => species.MaleHair,
                Gender.Female => species.FemaleHair,
                _ => species.MaleHair.Union(species.FemaleHair).ToList(),
            })
            .ToList();
        }

        profile.Appearance.HairStyleId = random.Pick(hairOptions);

        if (random.Prob(ShavenChance))
            profile.Appearance.FacialHairStyleId = HairStyles.DefaultFacialHairStyle;

        return profile;
    }

    private const float GenderlessFirstNameChance = 0.5f; // the future is woke
    private const float DoubleFirstNameChance = 0.015f;
    private const float HyphenatedFirstMiddleNameChance = 0.015f;
    private const float QuotedMiddleNameChance = 0.03f;
    private const float HyphenatedLastNameChance = 0.05f;
    private const float AbbreviatedMiddleChance = 0.07f;
    private const float AbbreviatedFirstMiddleChance = 0.07f;
    private const float AbbreviatedFirstMiddleAltChance = 0.4f;
    private const float ParticleChance = 0.025f;
    private const float SuffixChance = 0.05f;
    private const float PrefixChance = 0.07f;
    private const float PrefixGenderlessChance = 0.6f;
    private const float PrefixFirstNameless = 0.5f;
    private const float LastNameless = 0.009f;
    private const float FirstNameless = 0.006f;
    private const int AlliterationTotalChances = 6;

    private static readonly ProtoId<LocalizedDatasetPrototype> ParticleDataset = "ESNameParticle";
    private static readonly ProtoId<LocalizedDatasetPrototype> SuffixDataset = "ESNameSuffix";
    private static readonly ProtoId<LocalizedDatasetPrototype> PrefixGenderlessDataset = "ESNamePrefixGenderless";
    private static readonly ProtoId<LocalizedDatasetPrototype> PrefixMaleDataset = "ESNamePrefixMale";
    private static readonly ProtoId<LocalizedDatasetPrototype> PrefixFemaleDataset = "ESNamePrefixFemale";
    private static readonly ProtoId<LocalizedDatasetPrototype> PrefixNonbinaryDataset = "ESNamePrefixNonbinary";

    public void GenerateName(HumanoidCharacterProfile profile, SpeciesPrototype species)
    {
        var firstNameDataSet = _prototypeManager.Index(profile.Gender switch
        {
            Gender.Male => species.MaleFirstNames,
            Gender.Female => species.FemaleFirstNames,
            _ => _random.Pick(new []{species.FemaleFirstNames, species.GenderlessFirstNames, species.MaleFirstNames}),
        });

        if (_random.Prob(GenderlessFirstNameChance))
            firstNameDataSet = _prototypeManager.Index(species.GenderlessFirstNames);

        var lastNameDataSet = _prototypeManager.Index(species.LastNames);

        var prefix = Prefix(profile.Gender);
        var suffix = Suffix();
        var firstName = FirstName(firstNameDataSet);

        // when generating the lastname, we want to artificially boost the chance
        // that alliteration happens, because alliteration is usually really funny
        // we do this by essentially just generating the last name a few extra times
        // and if we generate an alliterative name, then we stop. otherwise, we just
        // take the last one that got generated
        var lastName = string.Empty;
        for (var i = 0; i < AlliterationTotalChances; i++)
        {
            lastName = LastName(lastNameDataSet);
            if (firstName.First() == lastName.First())
                break;
        }

        if (prefix != string.Empty && _random.Prob(PrefixFirstNameless))
            firstName = string.Empty;

        if (_random.Prob(LastNameless))
            lastName = string.Empty;
        else if (_random.Prob(FirstNameless))
            firstName = string.Empty;

        // double-spaces can occur when firstname/lastname are removed and a prefix/suffix exists
        profile.Name = $"{prefix} {firstName} {lastName} {suffix}".Trim().Replace("  ", " ");
    }

    private string Prefix(Gender gender)
    {
        if (!_random.Prob(PrefixChance))
            return string.Empty;

        var prefixDataSet = gender switch
        {
            Gender.Male => PrefixMaleDataset,
            Gender.Female => PrefixFemaleDataset,
            _ => PrefixNonbinaryDataset,
        };

        if (_random.Prob(PrefixGenderlessChance))
            prefixDataSet = PrefixGenderlessDataset;

        return _random.Pick(_prototypeManager.Index(prefixDataSet));
    }

    private string FirstName(LocalizedDatasetPrototype dataset, bool recursive = false)
    {
        var firstName = _random.Pick(dataset);

        if (_random.Prob(HyphenatedFirstMiddleNameChance))
        {
            firstName = Loc.GetString("es-name-hyphenation-fmt",
                ("first", _random.Pick(dataset)),
                ("second", _random.Pick(dataset)));
        }
        else if (_random.Prob(QuotedMiddleNameChance) && !recursive)
        {
            firstName = Loc.GetString("es-name-quoted-fmt",
                ("first", _random.Pick(dataset)),
                ("second", _random.Pick(dataset)));
        }

        if (_random.Prob(AbbreviatedMiddleChance) && !recursive)
        {
            firstName = Loc.GetString("es-name-middle-abbr-fmt", ("first", firstName), ("letter", RandomFirstLetter(dataset)));
        }
        else if (_random.Prob(AbbreviatedFirstMiddleChance))
        {
            var locId = _random.Prob(AbbreviatedFirstMiddleAltChance)
                ? "es-name-first-middle-abbr-fmt-alt"
                : "es-name-first-middle-abbr-fmt";
            firstName = Loc.GetString(locId, ("letter1", RandomFirstLetter(dataset)), ("letter2", RandomFirstLetter(dataset)));
        }

        // yes, this can generate some abominations
        if (_random.Prob(DoubleFirstNameChance))
        {
            firstName = Loc.GetString("es-name-normal-fmt", ("first", firstName), ("second", FirstName(dataset, true)));
        }

        return firstName;
    }

    private string LastName(LocalizedDatasetPrototype dataset)
    {
        var lastName = _random.Pick(dataset);

        if (_random.Prob(HyphenatedLastNameChance))
        {
            lastName = Loc.GetString("es-name-hyphenation-fmt",
                ("first", _random.Pick(dataset)),
                ("second", _random.Pick(dataset)));
        }

        if (_random.Prob(ParticleChance))
        {
            var particleDataSet = _prototypeManager.Index(ParticleDataset);
            lastName = Loc.GetString("es-name-normal-fmt",
                ("first", _random.Pick(particleDataSet)),
                ("second", lastName));
        }

        return lastName;
    }

    private string Suffix()
    {
        if (!_random.Prob(SuffixChance))
            return string.Empty;

        var suffixDataSet = _prototypeManager.Index(SuffixDataset);
        return _random.Pick(suffixDataSet);
    }

    private string RandomFirstLetter(LocalizedDatasetPrototype dataset)
    {
        return _random.Pick(dataset).Substring(0, 1);
    }
}
