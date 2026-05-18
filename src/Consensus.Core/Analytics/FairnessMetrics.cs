namespace Consensus.Core.Analytics;

/// <summary>
/// Pure functions for fairness and distribution metrics used by AnalyticsService and exports.
/// Definitions follow docs/METRICS_REFERENCE.md sections 4.2, 4.3, 14.2.
/// </summary>
public static class FairnessMetrics
{
    /// <summary>
    /// Gini coefficient on an integer count vector (e.g. blocks-per-node).
    /// 0 = perfect equality, 1 = one node holds everything.
    /// G = (2·Σ(i·xᵢ) − (n+1)·Σxᵢ) / (n·Σxᵢ)   with xᵢ sorted ascending, i starting at 1.
    /// Returns 0 for empty input or when every value is zero.
    /// </summary>
    public static double ComputeGini(IEnumerable<int> counts)
    {
        if (counts == null) return 0d;
        var sorted = counts.OrderBy(c => c).ToArray();
        var n = sorted.Length;
        if (n == 0) return 0d;

        long total = 0;
        long weighted = 0;
        for (int i = 0; i < n; i++)
        {
            total += sorted[i];
            weighted += (long)(i + 1) * sorted[i];
        }
        if (total == 0) return 0d;

        return (2d * weighted - (n + 1d) * total) / (n * (double)total);
    }

    /// <summary>
    /// Shannon entropy (in bits) on an integer count vector.
    /// H = −Σ pᵢ · log₂(pᵢ) for pᵢ = xᵢ / Σxⱼ. Zero-count buckets are skipped.
    /// </summary>
    public static double ComputeShannonEntropy(IEnumerable<int> counts)
    {
        if (counts == null) return 0d;
        var values = counts.Where(c => c > 0).ToArray();
        var total = values.Sum();
        if (total == 0) return 0d;

        double entropy = 0d;
        foreach (var v in values)
        {
            var p = v / (double)total;
            entropy -= p * Math.Log2(p);
        }
        return entropy;
    }

    /// <summary>
    /// Percentile via linear interpolation between adjacent ranks (NumPy default).
    /// <paramref name="percentile"/> is in [0, 100]. Empty input returns 0.
    /// </summary>
    public static double Percentile(IEnumerable<double> values, double percentile)
    {
        if (values == null) return 0d;
        var sorted = values.OrderBy(v => v).ToArray();
        var n = sorted.Length;
        if (n == 0) return 0d;
        if (n == 1) return sorted[0];

        var p = Math.Clamp(percentile, 0d, 100d) / 100d;
        var rank = p * (n - 1);
        var lower = (int)Math.Floor(rank);
        var upper = (int)Math.Ceiling(rank);
        if (lower == upper) return sorted[lower];

        var weight = rank - lower;
        return sorted[lower] * (1 - weight) + sorted[upper] * weight;
    }
}
