using Content.Shared.Atmos;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    /// <summary>
    /// Compares two <see cref="GasMixture"/>s and returns a similarity score between 0 and 1,
    /// based entirely on the percent compositions of the <see cref="GasMixture"/>s.
    /// </summary>
    /// <param name="a">The first <see cref="GasMixture"/> to compare.</param>
    /// <param name="b">The second <see cref="GasMixture"/> to compare.</param>
    /// <returns>A float between 0 and 1 based on how similar the gas mixtures are,
    /// based on percent compositions.</returns>
    public float GetGasMixtureSimilarity(GasMixture? a, GasMixture? b)
    {
        // Naive.
        if (a == null || b == null)
            return 0f;

        var aArr = a.Moles;
        var bArr = b.Moles;
        var len = aArr.Length;

        float numerator = 0f, denominatorA = 0f, denominatorB = 0f;

        // Preform a proportional overlap comparison between the two gas mixtures.
        for (var i = 0; i < len; i++)
        {
            numerator += Math.Min(aArr[i], bArr[i]);
            denominatorA += aArr[i];
            denominatorB += bArr[i];
        }

        var denominator = Math.Max(denominatorA, denominatorB);
        return denominator == 0f ? 1f : numerator / denominator;
    }

    /// <summary>
    /// Returns the percentage (0-1) of each gas in the mixture.
    /// </summary>
    public static float[] GetGasPercentages(GasMixture mixture)
    {
        var total = mixture.TotalMoles;
        var result = new float[mixture.Moles.Length];

        if (total <= 0f)
        {
            return result;
        }

        for (var i = 0; i < mixture.Moles.Length; i++)
        {
            result[i] = mixture.Moles[i] / total;
        }

        return result;
    }

    /// <summary>
    /// Returns true if all gases in the target mixture are present in the source mixture above a threshold percentage.
    /// </summary>
    public static bool HasGasesAboveThreshold(GasMixture target, GasMixture source, float threshold = 0.01f)
    {
        var targetPerc = GetGasPercentages(target);
        var sourcePerc = GetGasPercentages(source);

        // Check if each gas in the target mixture is present in the source mixture above the threshold.
        // If one doesn't match, we return false.
        for (var i = 0; i < targetPerc.Length; i++)
        {
            if (targetPerc[i] > 0f && sourcePerc[i] < targetPerc[i] - threshold)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Returns the minimum ratio of source/target for all gases present in the target mixture.
    /// </summary>
    public static float GetPurityRatio(GasMixture target, GasMixture source)
    {
        var targetPerc = GetGasPercentages(target);
        var sourcePerc = GetGasPercentages(source);
        var min = 1f;
        for (var i = 0; i < targetPerc.Length; i++)
        {
            if (targetPerc[i] > 0f)
            {
                if (sourcePerc[i] <= 0f)
                    return 0f;
                min = Math.Min(min, sourcePerc[i] / targetPerc[i]);
            }
        }
        return Math.Clamp(min, 0f, 1f);
    }

    /// <summary>
    /// Returns true if any gas in the target mixture is present in the source mixture above a threshold.
    /// </summary>
    public static bool HasAnyRequiredGas(GasMixture target, GasMixture source, float threshold = 0.01f)
    {
        var targetPerc = GetGasPercentages(target);
        var sourcePerc = GetGasPercentages(source);

        for (var i = 0; i < targetPerc.Length; i++)
        {
            if (targetPerc[i] > 0f && sourcePerc[i] > threshold)
                return true;
        }
        return false;
    }
}
