using System.Globalization;
using System.Linq;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server._ES.ServerStatus;

/// <summary>
///     This holds data for the random roleplay levels feature.
/// </summary>
[Prototype("esRoleplayLevels")]
public sealed partial class ESRoleplayLevelsPrototype : IPrototype, ISerializationHooks
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; set; } = default!;

    /// <summary>
    ///     Characters that roleplay levels are not allowed to start with.
    ///     These get validated and will cause validation failures.
    /// </summary>
    [DataField(required: true)]
    public List<string> ForbidCharacters = default!;

    /// <summary>
    ///     The kinds of roleplays in this dataset.
    /// </summary>
    [DataField(required: true)]
    public List<string> Roleplays = default!;

    void ISerializationHooks.AfterDeserialization()
    {
        var badRoleplays = new ValueList<string>();

        foreach (var roleplay in Roleplays)
        {
            if (CheckForbidCharactersViolation(roleplay))
                badRoleplays.Add(roleplay);
        }

        if (badRoleplays.Count > 0)
        {
            throw new Exception(
                $"Some roleplays in {ID} violate the forbidden characters: {string.Join(", ", badRoleplays)}");
        }
    }

    private bool CheckForbidCharactersViolation(string word)
    {
        return ForbidCharacters.Contains(word.GetRoleplayAbbreviation());
    }

    public string GetPossibleRoleplay(ILocalizationManager loc, IPrototypeManager proto, IRobustRandom random)
    {
        return random.Pick(Roleplays);
    }
}

public static class ESRoleplayLevelHelpers
{
    public static string GetRoleplayAbbreviation(this string level)
    {
        var titleCase = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(level);

        return string.Join(string.Empty, titleCase.ToCharArray().Where(char.IsUpper).ToList());
    }
}
