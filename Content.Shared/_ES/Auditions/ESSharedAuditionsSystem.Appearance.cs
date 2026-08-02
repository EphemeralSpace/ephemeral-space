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
using Content.Shared.Roles;
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
        Color.MediumPurple,
        Color.White,
        Color.ForestGreen,
        Color.LimeGreen,
        Color.DarkOrange,
        Color.IndianRed,
        Color.DarkKhaki,
        Color.Azure,
        Color.SteelBlue,
    ];

    public const float BaldChance = 0.01f;
    public const float CrazyHairChance = 0.10f;

    public const float ShavenChance = 0.55f;

    public const float YoungWeight = 4.5f;
    public const float MiddleAgeWeight = 3.5f;
    public const float OldAgeWeight = 2f;

    private static readonly ProtoId<LocalizedDatasetPrototype> DescriptorDataset = "ESCharacterDescriptor";
    private static readonly ProtoId<LocalizedDatasetPrototype> FocusDataset = "ESCharacterFocus";

    /// <summary>
    /// Generates a character with randomized name, age, gender and appearance.
    /// </summary>
    [PublicAPI]
    public Entity<MindComponent> GenerateCharacter(Entity<ESProducerComponent?> producer, ProtoId<JobPrototype>? job = null)
    {
        if (!Resolve(producer, ref producer.Comp))
            return _mind.CreateMind(null);

        var nameConfig = _prototypeManager.TryIndex(job, out var jobPrototype) ? jobPrototype.NameConfig : ESNameConfig.Default;
        var species = jobPrototype?.SpeciesOverride;

        var profile = RandomProfile(_random, species);

        profile.Name = GenerateName(nameConfig, profile.Gender, out var baseName);

        var ent = _mind.CreateMind(null, profile.Name);
        var character = EnsureComp<ESCharacterComponent>(ent);

        var year = _config.GetCVar(ESCVars.ESInGameYear) - profile.Age;
        var month = _random.Next(1, 12);
        var day = _random.Next(1, DateTime.DaysInMonth(year, month));
        character.DateOfBirth = new DateTime(year, month, day);
        character.Profile = profile;

        character.BaseName = baseName;

        character.Descriptor = Loc.GetString(_random.Pick(_prototypeManager.Index(DescriptorDataset)));
        character.Focus = Loc.GetString(_random.Pick(_prototypeManager.Index(FocusDataset)));

        if (producer.Comp.OpinionConcepts.Count >= 2)
        {
            var concepts = new List<LocId>(producer.Comp.OpinionConcepts);
            character.Likes.Add(_random.PickAndTake(concepts));
            character.Dislikes.Add(_random.PickAndTake(concepts));
        }

        character.Station = producer;

        Dirty(ent, character);

        producer.Comp.Characters.Add(ent);

        return ent;
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

        var skinColors = species.SkinColors.Select(_prototypeManager.Index).ToList();
        var weightedSkinColors = skinColors.Select(prototype => (prototype, prototype.Weight)).ToDictionary();

        var skinColor = random.Pick(weightedSkinColors);
        profile.Appearance.SkinColor = random.Pick(skinColor.Colors);

        profile.Age = random.Pick(new Dictionary<int, float>
        {
            { random.Next(species.MinAge, species.YoungAge), YoungWeight }, // Young age
            { random.Next(species.YoungAge, species.OldAge), MiddleAgeWeight }, // Middle age
            { random.Next(species.OldAge, species.MaxAge), OldAgeWeight }, // Old age
        });

        var hairColor = GenerateHairColor(profile, random);
        profile.Appearance.HairColor = hairColor;
        profile.Appearance.FacialHairColor = hairColor;

        var eyeColors = EyeColors.Where(c =>
        {
            var l = Color.ToHsl(c).Z;
            var otherL = Color.ToHsl(profile.Appearance.SkinColor).Z;
            return MathF.Abs(l - otherL) >= 0.20f;
        });
        profile.Appearance.EyeColor = random.Pick(eyeColors.ToList());

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

        if (hairOptions.Any())
            profile.Appearance.HairStyleId = random.Pick(hairOptions);
        if (random.Prob(BaldChance))
            profile.Appearance.HairStyleId = string.Empty; // This is awful but w/e

        if (random.Prob(ShavenChance))
        {
            profile.Appearance.FacialHairStyleId = HairStyles.DefaultFacialHairStyle;
        }
        else if (sex != Sex.Female)
        {
            var facialHairStyles = _marking.MarkingsByCategoryAndSpecies(MarkingCategories.FacialHair, speciesId).Keys.ToList();
            profile.Appearance.FacialHairStyleId = random.Pick(facialHairStyles);
        }

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

    public string GenerateName(ESNameConfig config, Gender gender, out string baseName)
    {
        var firstNameDataSet = _prototypeManager.Index(gender switch
        {
            Gender.Male => config.MaleFirstNames,
            Gender.Female => config.FemaleFirstNames,
            _ => _random.Pick(new []{config.FemaleFirstNames, config.GenderlessFirstNames, config.MaleFirstNames}),
        });

        if (_random.Prob(config.GenderlessFirstNameChance))
            firstNameDataSet = _prototypeManager.Index(config.GenderlessFirstNames);

        var lastNameDataSet = _prototypeManager.Index(config.LastNames);

        var prefix = Prefix(config, gender);
        var suffix = Suffix(config);
        var firstName = FirstName(config, firstNameDataSet);

        // when generating the lastname, we want to artificially boost the chance
        // that alliteration happens, because alliteration is usually really funny
        // we do this by essentially just generating the last name a few extra times
        // and if we generate an alliterative name, then we stop. otherwise, we just
        // take the last one that got generated
        var lastName = string.Empty;
        for (var i = 0; i < config.AlliterationTotalChances; i++)
        {
            lastName = LastName(config, lastNameDataSet);
            if (firstName.First() == lastName.First())
                break;
        }

        if (prefix != string.Empty && _random.Prob(config.PrefixFirstNameless))
            firstName = string.Empty;

        if (_random.Prob(config.LastNamelessChance))
            lastName = string.Empty;
        else if (_random.Prob(config.FirstNamelessChance))
            firstName = string.Empty;

        if (firstName != string.Empty && _random.Prob(config.AdjectiveFirstNameChance))
        {
            lastName = string.Empty;
            suffix = string.Empty;
            var adjectiveDataset = _prototypeManager.Index(config.NameAdjectiveDataset);

            for (var i = 0; i < config.AdjectiveAlliterationTotalChances; i++)
            {
                prefix = _random.Pick(adjectiveDataset);
                if (prefix.First() == firstName.First())
                    break;
            }
        }

        // double-spaces can occur when firstname/lastname are removed and a prefix/suffix exists
        baseName = $"{firstName} {lastName}".Replace("  ", " ");
        return $"{prefix} {firstName} {lastName} {suffix}".Trim().Replace("  ", " ");
    }

    private string Prefix(ESNameConfig config, Gender gender)
    {
        if (!_random.Prob(config.PrefixChance))
            return string.Empty;

        var prefixDataSet = gender switch
        {
            Gender.Male => config.PrefixMaleDataset,
            Gender.Female => config.PrefixFemaleDataset,
            _ => config.PrefixNonbinaryDataset,
        };

        if (_random.Prob(config.PrefixGenderlessChance))
            prefixDataSet = config.PrefixGenderlessDataset;

        return _random.Pick(_prototypeManager.Index(prefixDataSet));
    }

    private string FirstName(ESNameConfig config, LocalizedDatasetPrototype dataset, bool recursive = false)
    {
        var firstName = _random.Pick(dataset);

        if (_random.Prob(config.HyphenatedFirstMiddleNameChance))
        {
            firstName = Loc.GetString("es-name-hyphenation-fmt",
                ("first", _random.Pick(dataset)),
                ("second", _random.Pick(dataset)));
        }
        else if (_random.Prob(config.QuotedMiddleNameChance) && !recursive)
        {
            firstName = Loc.GetString("es-name-quoted-fmt",
                ("first", _random.Pick(dataset)),
                ("second", _random.Pick(dataset)));
        }

        if (_random.Prob(config.AbbreviatedMiddleChance) && !recursive)
        {
            firstName = Loc.GetString("es-name-middle-abbr-fmt", ("first", firstName), ("letter", RandomFirstLetter(dataset)));
        }
        else if (_random.Prob(config.AbbreviatedFirstMiddleChance))
        {
            var locId = _random.Prob(config.AbbreviatedFirstMiddleAltChance)
                ? "es-name-first-middle-abbr-fmt-alt"
                : "es-name-first-middle-abbr-fmt";
            firstName = Loc.GetString(locId, ("letter1", RandomFirstLetter(dataset)), ("letter2", RandomFirstLetter(dataset)));
        }

        // yes, this can generate some abominations
        if (_random.Prob(config.DoubleFirstNameChance))
        {
            firstName = Loc.GetString("es-name-normal-fmt", ("first", firstName), ("second", FirstName(config, dataset, true)));
        }

        if (_random.Prob(config.QuotedFirstNameChance))
        {
            firstName = firstName.Replace("\"", "");
            firstName = Loc.GetString("es-name-quoted-first-fmt", ("first", firstName));
        }

        return firstName;
    }

    private string LastName(ESNameConfig config, LocalizedDatasetPrototype dataset)
    {
        var lastName = _random.Pick(dataset);

        if (_random.Prob(config.HyphenatedLastNameChance))
        {
            lastName = Loc.GetString("es-name-hyphenation-fmt",
                ("first", _random.Pick(dataset)),
                ("second", _random.Pick(dataset)));
        }

        if (_random.Prob(config.ParticleChance))
        {
            var particleDataSet = _prototypeManager.Index(config.ParticleDataset);
            lastName = Loc.GetString("es-name-normal-fmt",
                ("first", _random.Pick(particleDataSet)),
                ("second", lastName));
        }

        return lastName;
    }

    private string Suffix(ESNameConfig config)
    {
        if (!_random.Prob(config.SuffixChance))
            return string.Empty;

        var suffixDataSet = _prototypeManager.Index(config.SuffixDataset);
        return _random.Pick(suffixDataSet);
    }

    private string RandomFirstLetter(LocalizedDatasetPrototype dataset)
    {
        return _random.Pick(dataset).Substring(0, 1);
    }
}
