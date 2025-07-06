using Robust.Shared.Random;

namespace Content.Shared._ES.Random;

public static class ESRandomHelpers
{
    public static Color NextColor(this IRobustRandom random, bool withAlpha = false)
    {
        return new Color(random.NextByte(), random.NextByte(), random.NextByte(), withAlpha ? random.NextByte() : byte.MaxValue);
    }
}
