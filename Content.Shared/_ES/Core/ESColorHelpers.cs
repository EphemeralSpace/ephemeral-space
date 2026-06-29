namespace Content.Shared._ES.Core;

public static class ESColorHelpers
{
    /// <summary>
    /// Returns a text color (white or black) that has the largest contrast for this color
    /// </summary>
    public static Color GetContrastTextColor(this Color c)
    {
        var (r, g, b) = c;

        // arbitrary formula that I definitely stole from somewhere random.
        return 0.2126f * r + 0.7152f * g + 0.0722f * b > 0.5
            ? Color.Black
            : Color.White;
    }
}
