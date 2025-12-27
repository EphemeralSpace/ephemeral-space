using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using SpaceWizards.Sodium;

namespace Content.Shared._Citadel.Utilities;

/// <summary>
///     A 128-bit seed for random number generation.
/// </summary>
/// <seealso cref="SmallRandom"/>
[Serializable, NetSerializable]
public readonly struct RngSeed
{
    private const ulong MagicScrambleConstant = 3141592653589793238;

    private readonly uint _s0, _s1, _s2, _s3;

    /// <summary>
    ///     Construct an RngSeed from the given 128-bit seed.
    /// </summary>
    [PublicAPI]
    public RngSeed(uint s0, uint s1, uint s2, uint s3)
    {
        _s0 = s0;
        _s1 = s1;
        _s2 = s2;
        _s3 = s3;
    }

    /// <summary>
    ///     Construct a SmallRandom from the given integer span.
    /// </summary>
    [PublicAPI]
    public RngSeed(Span<uint> span)
    {
        DebugTools.AssertEqual(span.Length, 4);
        _s0 = span[0];
        _s1 = span[1];
        _s2 = span[2];
        _s3 = span[3];
    }

    /// <summary>
    ///     Construct a SmallRandom from the given byte span.
    /// </summary>
    [PublicAPI]
    public RngSeed(Span<byte> span)
    {
        DebugTools.AssertEqual(span.Length, 16);
        // No memory marshal, gotta do this the lame way.

        _s0 = BitConverter.ToUInt32(span[0..4]);
        _s1 = BitConverter.ToUInt32(span[4..8]);
        _s2 = BitConverter.ToUInt32(span[8..12]);
        _s3 = BitConverter.ToUInt32(span[12..16]);
    }

    /// <summary>
    ///     Constructs an RngSeed using a SmallRandom as a basis.
    /// </summary>
    /// <param name="random">The randomizer to use.</param>
    /// <remarks>
    ///     This does <b>not</b> clone the randomizer, and calling Next() on this new randomizer is not equivalent to Next() on the old one.
    /// </remarks>
    [PublicAPI]
    public RngSeed(IRobustRandom random)
    {
        _s0 = (uint)random.Next();
        _s1 = (uint)random.Next();
        _s2 = (uint)random.Next();
        _s3 = (uint)random.Next();

        _s0 |= 0x70000000; // Set high bit to prevent zero issues.
    }

    /// <summary>
    ///     Directly create a mutable randomizer from this seed.
    ///     Multiple randomizers made from this seed will all behave identically and output the same sequence.
    /// </summary>
    [PublicAPI, Pure]
    public SmallRandom IntoRandomizer()
    {
        return new SmallRandom(_s0, _s1, _s2, _s3);
    }

    /// <summary>
    ///     Creates a new RngSeed, shuffled uniquely for the given coordinates.
    /// </summary>
    /// <param name="coordinates">World coordinates. This is PRECISE, please round appropriately.</param>
    /// <param name="level1">Arbitrary level number 1, probably your dimension or planet number, NOT a MapId.</param>
    /// <param name="level2">Arbitrary level number 2, something like your starsystem number or just zero.</param>
    /// <returns>A new rng seed for that coordinate, deterministically derived.</returns>
    /// <remarks>This isn't statistically sound or anything, just sufficient for procedural generation.</remarks>
    [PublicAPI, Pure]
    public RngSeed SeedForCoordinate(Vector2i coordinates, int level1, int level2)
    {
        var joinedXy = (((ulong)coordinates.X + 314159) << 32) | (uint)(coordinates.Y - 314159);
        var joinedLevel = (((ulong)level1 - 314159) << 32) | (uint)(level2 + 314159);

        // crongulate.
        joinedXy *= MagicScrambleConstant;
        joinedLevel *= MagicScrambleConstant;
        joinedXy ^= BitOperations.RotateLeft(joinedLevel, 17);
        joinedLevel ^= BitOperations.RotateLeft(joinedXy, 17);

        var s0 = _s0 + (uint)joinedXy;
        var s1 = _s1 + (uint)(joinedXy >> 32);
        var s2 = _s2 + (uint)joinedLevel;
        var s3 = _s3 + (uint)(joinedLevel >> 32);

        return new(s0, s1, s2, s3);
    }

    /// <summary>
    ///     Creates a new RngSeed, shuffled uniquely for the world coordinates and a unique ID.
    /// </summary>
    /// <param name="coordinates">World coordinates. This is PRECISE, please round appropriately.</param>
    /// <param name="uuid">An object's UUID.</param>
    /// <returns>A new rng seed for that coordinate, deterministically derived.</returns>
    /// <remarks>This isn't statistically sound or anything, just sufficient for procedural generation.</remarks>
    [PublicAPI, Pure]
    public RngSeed SeedForCoordinateAndUnique(Vector2i coordinates, Guid uuid)
    {
        DebugTools.Assert(uuid != Guid.Empty, "Empty UUIDs have poor statistical properties and should never be used for seed gen.");

        var joinedXy = (((ulong)coordinates.X + 314159) << 32) | (uint)(coordinates.Y - 314159);

        joinedXy *= MagicScrambleConstant;

        var bytes = uuid.ToByteArray(false);

        // yes, ideally we'd strip the uuid metadata, but shrug.
        var s0 = BitConverter.ToUInt32(bytes.AsSpan()[0..4]) | 0x70000000;
        var s1 = BitConverter.ToUInt32(bytes.AsSpan()[4..8]);
        var s2 = BitConverter.ToUInt32(bytes.AsSpan()[8..12]);
        var s3 = BitConverter.ToUInt32(bytes.AsSpan()[12..16]);

        // inner portion of xoroshiro..
        var t = s1 << 9;

        s2 ^= s0;
        s3 ^= s1;
        s1 ^= s2;
        s0 ^= s3;

        s2 ^= t;
        s3 = BitOperations.RotateLeft(s3, 11);

        // and integrate the coordinate.
        s0 += (uint)joinedXy;
        s1 += (uint)(joinedXy >> 32);
        s2 += (uint)joinedXy;
        s3 += (uint)(joinedXy >> 32);

        return new RngSeed(s0, s1, s2, s3);
    }


    /// <summary>
    ///     Create a new RngSeed, shuffled uniquely for the given 'step' (where step is an arbitrary integer but is assumed to be relatively small)
    /// </summary>
    /// <param name="step"></param>
    /// <returns></returns>
    /// <remarks>
    ///     Step does not have to be small, this code just takes extra steps to ensure decent rng spread.
    ///     As with <see cref="SeedForCoordinate"/>, this isn't proven statistically sound.
    /// </remarks>
    [PublicAPI, Pure]
    public RngSeed SeedForStep(int step)
    {
        step += 314159;
        var joinedStep = (((ulong)step) << 32) | (uint)step;

        joinedStep *= MagicScrambleConstant;
        joinedStep ^= BitOperations.RotateLeft(joinedStep, 17);

        var s0 = _s0 + (uint)joinedStep;
        var s1 = _s1 + (uint)(joinedStep >> 32);
        var s2 = _s2 + (uint)joinedStep;
        var s3 = _s3 + (uint)(joinedStep >> 32);

        return new(s0, s1, s2, s3);
    }

    /// <summary>
    ///     Construct a SmallRandom from the given string seed, as it would be in YAML.
    ///     This can be either a 128-bit number in hex, or a string that will be used byte-wise.
    ///     Directly compatible with the serialized form of SmallRandom, and safe to use against user input.
    /// </summary>
    /// <remarks>
    ///     Input string must not be empty, and user input seeds are not particularly random seeds.
    /// </remarks>
    [PublicAPI]
    public static bool TryFromStringAsSerialized(string seed, [NotNullWhen(true)] out RngSeed? rng)
    {
        DebugTools.Assert(seed.Length > 0);

        // Hex byte string.
        if (seed.Length == 32 && seed.All(char.IsAsciiHexDigit))
        {
            return TryFromStringAsHex(seed, out rng);
        }

        DebugTools.Assert(EncodingHelpers.UTF8.GetByteCount(seed) <= 16, "Oversized seed is being truncated before usage.");

        return TryFromStringAsSeed(seed, out rng);
    }

    /// <summary>
    ///     Creates a SmallRandom using the given 32-character hex string.
    /// </summary>
    /// <returns>Whether the hex string was successfully parsed.</returns>
    [PublicAPI]
    public static bool TryFromStringAsHex(string serialized, [NotNullWhen(true)] out RngSeed? rng)
    {
        if (serialized.Length != 32)
        {
            rng = null;
            return false;
        }

        rng = new RngSeed(
            uint.Parse(serialized[0..8], NumberStyles.HexNumber),
            uint.Parse(serialized[8..16], NumberStyles.HexNumber),
            uint.Parse(serialized[16..24], NumberStyles.HexNumber),
            uint.Parse(serialized[24..32], NumberStyles.HexNumber)
        );

        return true;
    }

    /// <summary>
    ///     Creates a SmallRandom using the bytes of the given string as a seed, with a safety to prevent seed 0.
    /// </summary>
    /// <returns>Whether the seed was successfully used.</returns>
    [PublicAPI]
    public static bool TryFromStringAsSeed(string seed, [NotNullWhen(true)] out RngSeed? rng)
    {
        if (seed.Length == 0)
        {
            rng = null;
            return false;
        }

        var utf8 = new byte[64];

        EncodingHelpers.UTF8.GetBytes(seed.AsSpan(), utf8.AsSpan());

        var hash = CryptoGenericHashBlake2B.Hash(16, utf8, ReadOnlySpan<byte>.Empty);

        rng = new(hash);

        return true;
    }

    /// <summary>
    ///     Debug equality. Do NOT use this unless you're writing an RNG test.
    ///     Not a stable API!
    /// </summary>
    [Pure]
    public bool DebugCheckByteEquality(ref RngSeed other)
    {
        return _s0 == other._s0 && _s1 == other._s1 && _s2 == other._s2 && _s3 == other._s3;
    }

    [Pure]
    [PublicAPI]
    public override string ToString()
    {
        return $"{_s0:X8}{_s1:X8}{_s2:X8}{_s3:X8}";
    }
}
