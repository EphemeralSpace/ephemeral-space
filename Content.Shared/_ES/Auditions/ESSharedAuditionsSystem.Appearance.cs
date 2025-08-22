using System.Linq;
using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.CCVar;
using Content.Shared._ES.Random;
using Content.Shared.Dataset;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
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

    public const float CrazyHairChance = 0.025f;

    public const float ShavenChance = 0.55f;

    private static readonly ProtoId<LocalizedDatasetPrototype> TendencyDataset = "ESPersonalityTendency";
    private static readonly ProtoId<LocalizedDatasetPrototype> TemperamentDataset = "ESPersonalityTemperament";

    /// <summary>
    /// Generates a character with randomized name, age, gender and appearance.
    /// </summary>
    public Entity<MindComponent, ESCharacterComponent> GenerateCharacter(Entity<ESProducerComponent> producer, [ForbidLiteral] string randomPrototype = "DefaultBackground")
    {
        var profile = HumanoidCharacterProfile.RandomWithSpecies();
        var species = _prototypeManager.Index(profile.Species);

        profile.Age = _random.Next(species.MinAge, species.MaxAge);

        IReadOnlyList<Color> hairColors;
        if (profile.Age >= species.OldAge)
            hairColors = RealisticAgedHairColors;
        else if (profile.Age <= species.YoungAge)
            hairColors = RealisticHairColors;
        else
            hairColors = RealisticHairColors.Union(RealisticAgedHairColors).ToList();

        var hairColor = _random.Prob(CrazyHairChance) ? _random.NextColor() : _random.Pick(hairColors);
        profile.Appearance.HairColor = hairColor;
        profile.Appearance.FacialHairColor = hairColor;

        List<ProtoId<MarkingPrototype>> hairOptions;
        if (_random.Prob(CrazyHairChance))
            hairOptions = species.UnisexHair.Union(species.FemaleHair).Union(species.MaleHair).ToList();
        else
            hairOptions = species.UnisexHair.Union(profile.Sex == Sex.Male ? species.MaleHair : species.FemaleHair).ToList();

        profile.Appearance.HairStyleId = _random.Pick(hairOptions);

        if (_random.Prob(ShavenChance))
            profile.Appearance.FacialHairStyleId = HairStyles.DefaultFacialHairStyle;

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
}
