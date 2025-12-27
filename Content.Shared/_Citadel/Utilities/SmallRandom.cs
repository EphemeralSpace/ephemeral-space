/*
The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.


This code is largely based on work from .NET's implementation of Random, put into a smaller formfactor and made compatible with IRobustRandom.
As such, this work is under their and only their license, despite being derivative.
https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Random.Xoshiro128StarStarImpl.cs
*/

using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Citadel.Utilities;

/// <summary>
///     An IRobustRandom compliant "small" RNG implementing xoroshiro128**.
/// </summary>
/// <remarks>
///     To set the seed, construct a new SmallRandom.
/// </remarks>
/// <seealso cref="RngSeed"/>
[Serializable, NetSerializable]
public sealed class SmallRandom : IRobustRandom
{
    private uint _s0, _s1, _s2, _s3;


    /// <summary>
    ///     Construct a SmallRandom from the given 128-bit seed.
    /// </summary>
    [PublicAPI]
    public SmallRandom(uint s0, uint s1, uint s2, uint s3)
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
    public SmallRandom(Span<uint> span)
    {
        DebugTools.AssertEqual(span.Length, 4);
        _s0 = span[0];
        _s1 = span[1];
        _s2 = span[2];
        _s3 = span[3];
    }

    /// <summary>
    ///     Constructs a SmallRandom using another source of randomness (i.e. the global RNG) as a basis.
    /// </summary>
    /// <param name="otherRandom">The other randomizer to use.</param>
    [PublicAPI]
    public SmallRandom(IRobustRandom otherRandom)
    {
        _s0 = (uint)otherRandom.Next();
        _s1 = (uint)otherRandom.Next();
        _s2 = (uint)otherRandom.Next();
        _s3 = (uint)otherRandom.Next();

        _s0 |= 0x70000000; // Set high bit to prevent zero issues.
    }

    /// <summary>
    ///     Constructs a SmallRandom using another SmallRandom as a basis.
    /// </summary>
    /// <param name="otherRandom">The other randomizer to use.</param>
    /// <remarks>
    ///     This does <b>not</b> clone the randomizer, and calling Next() on this new randomizer is not equivalent to Next() on the old one.
    /// </remarks>
    [PublicAPI]
    public SmallRandom(SmallRandom otherRandom)
    {
        _s0 = (uint)otherRandom.Next();
        _s1 = (uint)otherRandom.Next();
        _s2 = (uint)otherRandom.Next();
        _s3 = (uint)otherRandom.Next();

        _s0 |= 0x70000000; // Set high bit to prevent zero issues.
    }

    /*
     * Core methods.
     */

    /// <summary>
    ///     Returns the next uint in the pseudo-random sequence.
    /// </summary>
    /// <returns>An unsigned integer, of any valid bit pattern.</returns>
    [PublicAPI]
    public uint NextUInt32()
    {
        uint s0 = _s0, s1 = _s1, s2 = _s2, s3 = _s3;

        var result = BitOperations.RotateLeft(s1 * 5, 7) * 9;
        var t = s1 << 9;

        s2 ^= s0;
        s3 ^= s1;
        s1 ^= s2;
        s0 ^= s3;

        s2 ^= t;
        s3 = BitOperations.RotateLeft(s3, 11);

        _s0 = s0;
        _s1 = s1;
        _s2 = s2;
        _s3 = s3;

        return result;
    }

    // NextUInt32/64 algorithms based on https://arxiv.org/pdf/1805.10941.pdf and https://github.com/lemire/fastrange.

    /// <summary>
    ///     Returns a uint in the pseudo-random sequence that fits within the given range.
    /// </summary>
    /// <param name="maxValue">The maximum value of the [0, maxValue) range.</param>
    /// <returns>An unsigned integer, of any valid bit pattern less than <paramref name="maxValue"/>.</returns>
    [PublicAPI]
    public uint NextUInt32(uint maxValue)
    {
        ulong randomProduct = (ulong)maxValue * NextUInt32();
        uint lowPart = (uint)randomProduct;

        if (lowPart < maxValue)
        {
            uint remainder = unchecked(0u - maxValue) % maxValue;

            while (lowPart < remainder)
            {
                randomProduct = (ulong)maxValue * NextUInt32();
                lowPart = (uint)randomProduct;
            }
        }

        return (uint)(randomProduct >> 32);
    }

    /// <summary>
    ///     Returns a ulong in the pseudo-random sequence that fits within the given range.
    /// </summary>
    /// <param name="maxValue">The maximum value of the [0, maxValue) range.</param>
    /// <returns>An unsigned integer, of any valid bit pattern less than <paramref name="maxValue"/>.</returns>
    [PublicAPI]
    public ulong NextUInt64(ulong maxValue)
    {
        ulong randomProduct = Math.BigMul(maxValue, NextUInt64(), out ulong lowPart);

        if (lowPart < maxValue)
        {
            ulong remainder = unchecked(0ul - maxValue) % maxValue;

            while (lowPart < remainder)
            {
                randomProduct = Math.BigMul(maxValue, NextUInt64(), out lowPart);
            }
        }

        return randomProduct;
    }

    /// <summary>
    ///     Returns the next ulong in the pseudo-random sequence.
    /// </summary>
    /// <returns>An unsigned long integer, of any valid bit pattern.</returns>
    [PublicAPI]
    public ulong NextUInt64() => (((ulong)NextUInt32()) << 32) | NextUInt32();

    /*
     * Public API.
     */

    /// <summary>
    ///     Returns the next long in the pseudo-random sequence.
    /// </summary>
    /// <returns>A long integer, in the range [0, long.MaxValue) exclusive.</returns>
    [PublicAPI]
    public long NextInt64()
    {
        while (true)
        {
            // Get top 63 bits to get a value in the range [0, long.MaxValue], but try again
            // if the value is actually long.MaxValue, as the method is defined to return a value
            // in the range [0, long.MaxValue).
            ulong result = NextUInt64() >> 1;
            if (result != long.MaxValue)
            {
                return (long)result;
            }
        }
    }

    /// <summary>
    ///     Returns the next long in the pseudo-random sequence within a given range.
    /// </summary>
    /// <returns>A long integer, in the range [0, <paramref name="maxValue"/>) exclusive.</returns>
    /// <remarks>
    ///     <paramref name="maxValue"/> must not be negative.
    /// </remarks>
    [PublicAPI]
    public long NextInt64(long maxValue)
        => NextInt64(0, maxValue);

    /// <summary>
    ///     Returns the next long in the pseudo-random sequence within a given range.
    /// </summary>
    /// <returns>A long integer, in the range [<paramref name="minValue"/>, <paramref name="maxValue"/>) exclusive.</returns>
    /// <remarks>
    ///     The span must not be reversed (i.e. <paramref name="minValue"/> &lt; <paramref name="maxValue"/>)
    /// </remarks>
    [PublicAPI]
    public long NextInt64(long minValue, long maxValue)
    {
        ulong exclusiveRange = (ulong)(maxValue - minValue);

        if (exclusiveRange <= int.MaxValue)
        {
            return Next((int)exclusiveRange) + minValue;
        }

        // Narrow down to the smallest range [0, 2^bits] that contains maxValue.
        // Then repeatedly generate a value in that outer range until we get one within the inner range.
        int bits = BitHelpers.Log2Ceiling(exclusiveRange);
        while (true)
        {
            ulong result = NextUInt64() >> (sizeof(ulong) * 8 - bits);
            if (result < exclusiveRange)
            {
                return (long)result + minValue;
            }
        }
    }

    /*
     * Interface implementations.
     */

    /// <inheritdoc/>
    [Obsolete("SmallRandom does not use a system random internally, so this method always throws.")]
    public System.Random GetRandom()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    [Obsolete("IRobustRandom interface isn't suitable for setting the seed, so this method always throws.")]
    public void SetSeed(int seed)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    [PublicAPI]
    public float NextFloat()
    {
        return (NextUInt32() >> 8) * (1.0f / (1u << 24));
    }

    /// <inheritdoc/>
    [PublicAPI]
    public int Next()
    {
        while (true)
        {
            // Get top 31 bits to get a value in the range [0, int.MaxValue], but try again
            // if the value is actually int.MaxValue, as the method is defined to return a value
            // in the range [0, int.MaxValue).
            var result = NextUInt32() >> 1;
            if (result != int.MaxValue)
            {
                return (int)result;
            }
        }
    }

    /// <inheritdoc/>
    [PublicAPI]
    public int Next(int maxValue)
    {
        DebugTools.Assert(maxValue >= 0, "maxValue must not be negative or zero.");

        return (int)NextUInt32((uint)maxValue);
    }

    /// <inheritdoc/>
    [PublicAPI]
    public int Next(int minValue, int maxValue)
    {
        DebugTools.Assert(minValue <= maxValue, "The span must not be reversed.");

        return (int)NextUInt32((uint)(maxValue - minValue)) + minValue;
    }

    /// <inheritdoc/>
    [PublicAPI]
    public double NextDouble()
    {
        return (NextUInt64() >> 11) * (1.0 / (1ul << 53));
    }

    /// <inheritdoc/>
    [PublicAPI]
    public TimeSpan Next(TimeSpan maxTime)
    {
        return Next(TimeSpan.Zero, maxTime);
    }

    /// <inheritdoc/>
    [PublicAPI]
    public TimeSpan Next(TimeSpan minTime, TimeSpan maxTime)
    {
        return minTime + (maxTime - minTime) * NextDouble();
    }

    /// <inheritdoc/>
    [PublicAPI]
    public void NextBytes(byte[] buffer)
    {
        NextBytes(buffer.AsSpan());
    }

    /// <summary>
    ///     Fills the given buffer with pseudo-random data.
    /// </summary>
    /// <param name="buffer">The span to modify.</param>
    [PublicAPI]
    public void NextBytes(Span<byte> buffer)
    {
        // todo optimize

        while (buffer.Length >= sizeof(ulong))
        {
            var value = NextUInt64();

            BitConverter.TryWriteBytes(buffer, value);

            buffer = buffer[sizeof(ulong)..];
        }

        var lastValue = NextUInt64();

        while (buffer.Length > 0)
        {
            buffer[0] = (byte)(lastValue & 0xFF);
            lastValue >>= 8;
            buffer = buffer[1..];
        }
    }

    public void Shuffle<T>(IList<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n -= 1;
            var k = Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    public void Shuffle<T>(Span<T> list)
    {
        var n = list.Length;
        while (n > 1)
        {
            n -= 1;
            var k = Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    public void Shuffle<T>(ValueList<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n -= 1;
            var k = Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    /// <summary>
    ///     Debug equality. Do NOT use this unless you're writing an RNG test.
    ///     Not a stable API!
    /// </summary>
    [Pure]
    public bool DebugCheckByteEquality(SmallRandom other)
    {
        return _s0 == other._s0 && _s1 == other._s1 && _s2 == other._s2 && _s3 == other._s3;
    }

    [PublicAPI, Pure]
    public override string ToString()
    {
        return $"{_s0:X8}{_s1:X8}{_s2:X8}{_s3:X8}";
    }
}
