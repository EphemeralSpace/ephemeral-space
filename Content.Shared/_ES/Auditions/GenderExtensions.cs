using Robust.Shared.Enums;

namespace Content.Shared._ES.Auditions;

public static class GenderExtensions
{
    public static string GetSymbol(this Gender gender)
    {
        return gender switch
        {
            Gender.Male => "♂",
            Gender.Female => "♀",
            Gender.Epicene => "⚥",
            _ => "⚲",
        };
    }

    public static string GetPronounString(this Gender gender, ILocalizationManager loc)
    {
        return loc.GetString($"humanoid-profile-editor-pronouns-{gender.ToString().ToLower()}-text");
    }
}
