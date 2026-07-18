using Content.Shared.Destructible.Thresholds;
using Content.Shared.FixedPoint;
using Robust.Shared.Random;

namespace Content.Shared._ES.Random;

public static class ESRandomHelpers
{
    public static Color NextColor(this IRobustRandom random, bool withAlpha = false)
    {
        return new Color(random.NextByte(), random.NextByte(), random.NextByte(), withAlpha ? random.NextByte() : byte.MaxValue);
    }

    public static int Next(this IRobustRandom random, MinMax minMax)
    {
        return random.Next(minMax.Min, minMax.Max + 1);
    }

    public static FixedPoint2 Next(this IRobustRandom random, FixedPoint2 min, FixedPoint2 max)
    {
        return FixedPoint2.FromRaw(random.Next(min.Value, max.Value));
    }
}
