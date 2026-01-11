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
using Content.Shared.Random.Helpers;
using JetBrains.Annotations;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// The main system for handling the creation, integration of relations
/// </summary>
public abstract partial class ESSharedAuditionsSystem
{
    /// <summary>
    /// Eye colors, selected for variance and contrast with human skin tones
    /// </summary>
    public static readonly IReadOnlyList<Color> EyeColors =
    [
        Color.Black,
        Color.Gray,
        Color.MediumPurple,
        Color.FromHex("#f29bdf"), // Light Pink
        Color.White,
        Color.ForestGreen,
        Color.LimeGreen,
        Color.DarkOrange,
        Color.IndianRed,
        Color.DarkKhaki,
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
    [PublicAPI]
    public Entity<MindComponent, ESCharacterComponent> GenerateCharacter(Entity<ESProducerComponent> producer)
    {
        var profile = RandomProfile(_random);
        var species = _prototypeManager.Index(profile.Species);

        GenerateName(profile, species, out var baseName);

        var (ent, mind) = _mind.CreateMind(null, profile.Name);
        var character = EnsureComp<ESCharacterComponent>(ent);

        var year = _config.GetCVar(ESCVars.ESInGameYear) - profile.Age;
        var month = _random.Next(1, 12);
        var day = _random.Next(1, DateTime.DaysInMonth(year, month));
        character.DateOfBirth = new DateTime(year, month, day);
        character.Profile = profile;

        character.BaseName = baseName;

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
            _ => strategy.ClosestSkinColor(random.NextColor()),
        };

        profile.Age = random.Pick(new Dictionary<int, float>
        {
            { random.Next(species.MinAge, species.YoungAge), YoungWeight }, // Young age
            { random.Next(species.YoungAge, species.OldAge), MiddleAgeWeight }, // Middle age
            { random.Next(species.OldAge, species.MaxAge), OldAgeWeight }, // Old age
        });

        var hairColor = GenerateHairColor(profile, random);
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

    public Color GenerateHairColor(HumanoidCharacterProfile profile, IRobustRandom random)
    {
        if (random.Prob(CrazyHairChance))
            return random.NextColor();

        var colors = new Dictionary<ESHairColorPrototype, float>();
        foreach (var colorProto in _prototypeManager.EnumeratePrototypes<ESHairColorPrototype>())
        {
            if (colorProto.Abstract)
                continue;

            if (profile.Age < colorProto.MinAge || profile.Age > colorProto.MaxAge)
                continue;

            colors.Add(colorProto, colorProto.Weight);
        }

        var colorType = random.Pick(colors);
        var color = random.Pick(colorType.Colors);
        return color;
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

    public void GenerateName(HumanoidCharacterProfile profile, SpeciesPrototype species) => GenerateName(profile, species, out _);

    public void GenerateName(HumanoidCharacterProfile profile, SpeciesPrototype species, out string baseName)
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
        baseName = $"{firstName} {lastName}".Replace("  ", " ");
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
