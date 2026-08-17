using System.Linq;
using Content.Shared.Decals;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Chat;

public abstract partial class ESSharedChatSystem
{
    private static readonly ProtoId<ColorPalettePrototype> ChatNamePalette = "ChatNames";
    private Color[] _chatNameColors = default!;

    private void InitializeNameColor()
    {
        var nameColors = _prototype.Index(ChatNamePalette).Colors.Values.OrderBy(c => c.ToHex()).ToArray();
        _chatNameColors = new Color[nameColors.Length];
        for (var i = 0; i < nameColors.Length; i++)
        {
            _chatNameColors[i] = nameColors[i];
        }
    }

    /// <summary>
    /// Returns a name color based on a string.
    /// Not unique per entity, but rather per name. Two entities named "monkey" will have identical colors.
    /// </summary>
    public Color GetChatColor(string name)
    {
        DebugTools.Assert(_chatNameColors.Length != 0);

        // We use a non-seeded hash algorithm so it's the same between server and clients
        // Default C# .GetHashCode() is seeded per-process
        var hash = (int) Adler32(name);

        var colorIdx = MathHelper.Mod(hash, _chatNameColors.Length);
        return _chatNameColors[colorIdx];

        // From https://gist.github.com/i-e-b/c37cc2d728fe5e5a56205cd7e62d682c
        static uint Adler32(string str)
        {
            const int mod = 65521;
            uint a = 1, b = 0;
            foreach (var c in str)
            {
                a = (a + c) % mod;
                b = (b + a) % mod;
            }
            return (b << 16) | a;
        }
    }

    /// <summary>
    /// Returns a color to be used for outlining a name color.
    /// Just a slightly darker version of the regular chat color.
    /// </summary>
    /// <param name="nameColor">The name color, ideally from <see cref="GetChatColor"/></param>
    /// <returns>A color to use for outlining text of the name color</returns>
    public Color GetChatOutlineColor(Color nameColor)
    {
        var (l, c, h, a) = Color.ToLch(Color.ToLab(nameColor));
        l = MathF.Max(l - 0.4f, 0.2f);
        return Color.FromLab(Color.FromLch(new(l, c, h, a)));
    }
}
