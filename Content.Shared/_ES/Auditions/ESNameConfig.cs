using Content.Shared.Dataset;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// Holds information related to generating random names
/// </summary>
[DataDefinition]
public partial struct ESNameConfig
{
    public static ESNameConfig Default => new();

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> MaleFirstNames { get; private set; } = "ESNamesFirstMale";

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> FemaleFirstNames { get; private set; } = "ESNamesFirstFemale";

    /// <remarks>
    /// This doesn't mean 'Stereotypical Nonbinary Names' or whatever this just means names anyone could have.
    /// </remarks>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> GenderlessFirstNames { get; private set; } = "ESNamesFirstGenderless";

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> LastNames { get; private set; } = "ESNamesLast";

    /// <summary>
    /// Chance that the first name dataset used will solely be <see cref="SpeciesPrototype.GenderlessFirstNames"/>
    /// </summary>
    [DataField]
    public float GenderlessFirstNameChance = 0.6f; // the future is woke

    /// <summary>
    /// Chance that the first name will recursively generate a second first name.
    /// Note that this second first name will not have embellishments like the first
    /// </summary>
    [DataField]
    public float DoubleFirstNameChance = 0.01f;

    /// <summary>
    /// Chance that the first name will have an attached hyphenated middle name
    /// </summary>
    /// <example>
    /// Jean-Luc, Beef-Steak
    /// </example>
    [DataField]
    public float HyphenatedFirstMiddleNameChance = 0.01f;

    /// <summary>
    /// Chance that the first name will have a quoted nickname appended to it
    /// </summary>
    /// <example>
    /// Ricky "Fuck", Jim "Boner"
    /// </example>
    [DataField]
    public float QuotedMiddleNameChance = 0.01f;

    /// <summary>
    /// Chance that the last name will be hyphenated
    /// </summary>
    /// <example>
    /// Whatsapp-Fury, Dennis-Crater
    /// </example>
    [DataField]
    public float HyphenatedLastNameChance = 0.025f;

    /// <summary>
    /// Chance that the first name will have an abbreviated middle name following it
    /// </summary>
    /// <example>
    /// Gibb P., Rigel F.
    /// </example>
    [DataField]
    public float AbbreviatedMiddleChance = 0.055f;

    /// <summary>
    /// Chance that the first name will be converted into a pair of abbreviations.
    /// </summary>
    /// <example>
    /// R.L., FE, J.K.
    /// </example>
    [DataField]
    public float AbbreviatedFirstMiddleChance = 0.065f;

    /// <summary>
    /// Given <see cref="AbbreviatedFirstMiddleChance"/>, chance that the initials are formatted as "AB" instead of "A.B."
    /// </summary>
    [DataField]
    public float AbbreviatedFirstMiddleAltChance = 0.4f;

    /// <summary>
    /// Chance that a particle will be inserted between the first and last name
    /// </summary>
    [DataField]
    public float ParticleChance = 0.025f;

    /// <summary>
    /// Chance that the name will be followed by a suffix
    /// </summary>
    [DataField]
    public float SuffixChance = 0.04f;

    /// <summary>
    /// Chance that the name will be preceded by a prefix
    /// </summary>
    [DataField]
    public float PrefixChance = 0.07f;

    /// <summary>
    /// Given <see cref="PrefixChance"/>, chance that the prefix dataset will be overriden with <see cref="PrefixGenderlessDataset"/>
    /// </summary>
    [DataField]
    public float PrefixGenderlessChance = 0.01f;

    /// <summary>
    /// Given <see cref="PrefixChance"/>, chance that there will be no first name
    /// </summary>
    [DataField]
    public float PrefixFirstNameless = 0.8f;

    /// <summary>
    /// Chance that there will be no last name
    /// </summary>
    [DataField]
    public float LastNamelessChance = 0.018f;

    /// <summary>
    /// Chance that there will be no first name
    /// </summary>
    [DataField]
    public float FirstNamelessChance = 0.009f;

    /// <summary>
    /// Chance that a name will generate a random adjective from <see cref="NameAdjectiveDataset"/> as a prefix
    /// </summary>
    [DataField]
    public float AdjectiveFirstNameChance = 0.035f;

    /// <summary>
    /// Number of attempted last name generations to create an alliterative first name + last name combo
    /// </summary>
    [DataField]
    public int AlliterationTotalChances = 6;

    /// <summary>
    /// Number of attempted adjective generations to create an alliterative adjective + first name combo
    /// </summary>
    [DataField]
    public int AdjectiveAlliterationTotalChances = 3;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> ParticleDataset = "ESNameParticle";

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> SuffixDataset = "ESNameSuffix";

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> PrefixGenderlessDataset = "ESNamePrefixGenderless";

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> PrefixMaleDataset = "ESNamePrefixMale";

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> PrefixFemaleDataset = "ESNamePrefixFemale";

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> PrefixNonbinaryDataset = "ESNamePrefixNonbinary";

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> NameAdjectiveDataset = "ESNameAdjectives";
}
