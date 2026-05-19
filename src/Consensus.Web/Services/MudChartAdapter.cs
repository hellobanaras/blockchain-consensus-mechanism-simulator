using Consensus.Core.Models;
using MudBlazor;

namespace Consensus.Web.Services;

/// <summary>
/// Translates Consensus.Core analytics DTOs into the shapes MudChart wants
/// (<see cref="ChartSeries"/> arrays, label arrays, raw double arrays for
/// pie/donut). Keeping this conversion in one place stops every razor chart
/// component from re-deriving the same data per render and makes it cheap to
/// swap chart libraries again later — only this adapter changes.
/// </summary>
public static class MudChartAdapter
{
    /// <summary>
    /// Build a single-series Line/Bar chart input from a round-duration series.
    /// </summary>
    public static (List<ChartSeries> Series, string[] Labels) ToRoundDurationSeries(
        IEnumerable<RoundDurationPoint> points,
        string seriesName = "Round duration (ms)")
    {
        var list = points?.ToList() ?? new List<RoundDurationPoint>();
        var labels = list.Select(p => p.Round.ToString()).ToArray();
        var series = new List<ChartSeries>
        {
            new ChartSeries
            {
                Name = seriesName,
                Data = list.Select(p => p.DurationMs).ToArray(),
            }
        };
        return (series, labels);
    }

    /// <summary>
    /// Build pie/donut inputs (double[] values, string[] labels) from a
    /// leader-distribution tally.
    /// </summary>
    public static (double[] Data, string[] Labels) ToPieData(LeaderDistribution dist)
    {
        if (dist?.Counts == null || dist.Counts.Count == 0)
        {
            return (Array.Empty<double>(), Array.Empty<string>());
        }
        var ordered = dist.Counts.OrderByDescending(kvp => kvp.Value).ToList();
        return (
            ordered.Select(kvp => (double)kvp.Value).ToArray(),
            ordered.Select(kvp => kvp.Key).ToArray()
        );
    }

    /// <summary>
    /// Build a multi-series ChartSeries[] keyed by series name from time-series
    /// data points. The selector picks one metric per point.
    /// </summary>
    public static (List<ChartSeries> Series, string[] Labels) ToTimeSeries(
        IEnumerable<Consensus.Core.Models.TimeSeriesDataPoint> points,
        Func<Consensus.Core.Models.TimeSeriesDataPoint, double> valueSelector,
        string seriesName,
        string labelFormat = "HH:mm")
    {
        var list = points?.OrderBy(p => p.Timestamp).ToList() ?? new List<Consensus.Core.Models.TimeSeriesDataPoint>();
        var labels = list.Select(p => p.Timestamp.ToLocalTime().ToString(labelFormat)).ToArray();
        var series = new List<ChartSeries>
        {
            new ChartSeries
            {
                Name = seriesName,
                Data = list.Select(valueSelector).ToArray(),
            }
        };
        return (series, labels);
    }

    /// <summary>
    /// Bin a flat list of values into <paramref name="binCount"/> equal-width
    /// buckets and return the frequencies + human-readable labels. Used to
    /// adapt histogram-shaped data (e.g. PoET wait times, processing latencies)
    /// into MudChart Bar inputs.
    /// </summary>
    public static (string[] Labels, double[] Frequencies, List<HistogramBin> Bins) BinForHistogram(
        IEnumerable<double> values,
        int binCount = 10,
        string unitSuffix = "")
    {
        var arr = values?.Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).ToArray()
                  ?? Array.Empty<double>();
        if (arr.Length == 0 || binCount < 1)
        {
            return (Array.Empty<string>(), Array.Empty<double>(), new List<HistogramBin>());
        }

        var min = arr.Min();
        var max = arr.Max();
        if (Math.Abs(max - min) < 1e-9)
        {
            // All values identical — one bucket with full count.
            var label = $"{min:F1}{unitSuffix}";
            return (new[] { label }, new[] { (double)arr.Length },
                new List<HistogramBin> { new(label, arr.Length, min, max) });
        }

        var width = (max - min) / binCount;
        var bins = new List<HistogramBin>(binCount);
        for (int i = 0; i < binCount; i++)
        {
            var lower = min + i * width;
            var upper = (i == binCount - 1) ? max : lower + width;
            // Last bin is inclusive at the upper bound to capture max.
            var count = arr.Count(v => v >= lower && (i == binCount - 1 ? v <= upper : v < upper));
            var label = $"{lower:F1}–{upper:F1}{unitSuffix}";
            bins.Add(new HistogramBin(label, count, lower, upper));
        }
        return (
            bins.Select(b => b.Label).ToArray(),
            bins.Select(b => b.Frequency).ToArray(),
            bins
        );
    }

    /// <summary>
    /// Build a protocol-comparison Bar chart from <see cref="ProtocolComparisonPoint"/>
    /// rows. <paramref name="metricSelector"/> picks which metric to plot
    /// (mean block time, Gini, success rate, …).
    /// </summary>
    public static (List<ChartSeries> Series, string[] Labels) ToProtocolBar(
        IEnumerable<ProtocolComparisonPoint> rows,
        Func<ProtocolComparisonPoint, double> metricSelector,
        string seriesName)
    {
        var list = rows?.ToList() ?? new List<ProtocolComparisonPoint>();
        var labels = list.Select(p => p.Protocol.ToString()).ToArray();
        var series = new List<ChartSeries>
        {
            new ChartSeries
            {
                Name = seriesName,
                Data = list.Select(metricSelector).ToArray(),
            }
        };
        return (series, labels);
    }
}
