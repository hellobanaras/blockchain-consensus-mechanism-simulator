using Consensus.Core.Enums;

namespace Consensus.Core.Models;

/// <summary>
/// Tally of how many blocks each node proposed within a single simulation.
/// Used by the SimulationDashboard donut and SpecializedAnalytics leader-distribution charts.
/// </summary>
public record LeaderDistribution(IReadOnlyDictionary<string, int> Counts)
{
    public int Total => Counts.Values.Sum();
}

/// <summary>
/// One point on the round-duration time series for a simulation.
/// </summary>
public record RoundDurationPoint(long Round, double DurationMs);

/// <summary>
/// One row in the per-simulation block timeline. <see cref="ProposerName"/> is
/// pre-resolved by AnalyticsService so chart components don't need a Node lookup.
/// </summary>
public record BlockTimelinePoint(
    DateTime Timestamp,
    long BlockNumber,
    Guid? ProposerId,
    string ProposerName);

/// <summary>
/// One bucket in a binned histogram (e.g. PoET wait-time distribution).
/// </summary>
public record HistogramBin(string Label, double Frequency, double LowerBound, double UpperBound);

/// <summary>
/// One row in a protocol-comparison table (used by PerformanceBaselines).
/// Pulled from <see cref="AlgorithmPerformanceMetrics"/> but flattened so charts
/// can iterate without re-projecting per render.
/// </summary>
public record ProtocolComparisonPoint(
    ConsensusAlgorithm Protocol,
    double MeanBlockTimeMs,
    double P95BlockTimeMs,
    double P99BlockTimeMs,
    double LeaderGini,
    double LeaderEntropy,
    double SuccessRate,
    int TotalSimulations);
