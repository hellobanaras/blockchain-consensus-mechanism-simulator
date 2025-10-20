using Consensus.Core.Entities;
using Consensus.Core.Enums;
using Consensus.Core.Repositories;
using Consensus.Core.Models;
using Microsoft.Extensions.Logging;

namespace Consensus.Core.Services;

/// <summary>
/// Service for generating analytics and metrics from simulation data
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Generate comprehensive analytics summary
    /// </summary>
    Task<AnalyticsSummary> GenerateAnalyticsSummaryAsync(AnalyticsRequest? request = null);

    /// <summary>
    /// Get winner distribution for a specific simulation
    /// </summary>
    Task<Dictionary<string, NodeWinnerStats>> GetWinnerDistributionAsync(Guid simulationId);

    /// <summary>
    /// Get performance metrics by consensus algorithm
    /// </summary>
    Task<Dictionary<ConsensusAlgorithm, AlgorithmPerformanceMetrics>> GetAlgorithmPerformanceAsync();

    /// <summary>
    /// Get time-series data for charts
    /// </summary>
    Task<List<TimeSeriesDataPoint>> GetTimeSeriesDataAsync(DateTime startDate, DateTime endDate, int bucketMinutes = 30);

    /// <summary>
    /// Export analytics data in various formats (CSV, JSON, etc.)
    /// </summary>
    Task<byte[]> ExportAnalyticsAsync(AnalyticsRequest request, string format);
}

/// <summary>
/// Implementation of analytics service for generating comprehensive simulation metrics
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly ISimulationRunRepository _simulationRunRepository;
    private readonly IBlockRepository _blockRepository;
    private readonly INodeRepository _nodeRepository;
    private readonly IConsensusRoundRepository _consensusRoundRepository;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        ISimulationRunRepository simulationRunRepository,
        IBlockRepository blockRepository,
        INodeRepository nodeRepository,
        IConsensusRoundRepository consensusRoundRepository,
        IEventLogRepository eventLogRepository,
        ILogger<AnalyticsService> logger)
    {
        _simulationRunRepository = simulationRunRepository;
        _blockRepository = blockRepository;
        _nodeRepository = nodeRepository;
        _consensusRoundRepository = consensusRoundRepository;
        _eventLogRepository = eventLogRepository;
        _logger = logger;
    }

    public async Task<AnalyticsSummary> GenerateAnalyticsSummaryAsync(AnalyticsRequest? request = null)
    {
        request ??= new AnalyticsRequest();
        
        _logger.LogInformation("Generating analytics summary for request {@Request}", request);

        // Get simulation data
        var simulations = await GetFilteredSimulationsAsync(request);
        
        // Filter simulations by optional criteria
        if (request.AlgorithmFilter?.Any() == true)
        {
            simulations = simulations.Where(s => request.AlgorithmFilter.Contains(s.ConsensusAlgorithm.ToString())).ToList();
        }

        // Get metrics
        var totalBlocks = await CountBlocksBySimulationsAsync(simulations);
        var totalNodes = 0;
        
        foreach (var sim in simulations)
        {
            var nodes = await _nodeRepository.GetBySimulationRunAsync(sim.Id);
            totalNodes += nodes.Count();
        }

        var summary = new AnalyticsSummary
        {
            TotalSimulations = simulations.Count(),
            TotalBlocks = totalBlocks,
            TotalNodes = totalNodes,
            AverageBlocksPerSimulation = simulations.Any() ? (double)totalBlocks / simulations.Count() : 0,
            AverageSimulationDuration = simulations.Any() ? 
                simulations.Where(s => s.CompletedAt.HasValue && s.StartedAt.HasValue).Average(s => (s.CompletedAt!.Value - s.StartedAt!.Value).TotalSeconds) : 0,
            StartDate = request.StartDate ?? simulations.Min(s => s?.StartedAt) ?? DateTime.UtcNow.AddDays(-30),
            EndDate = request.EndDate ?? simulations.Max(s => s?.CompletedAt) ?? DateTime.UtcNow
        };

        // Calculate additional metrics
        summary.StatusDistribution = GetStatusDistribution(simulations);
        summary.MostUsedAlgorithm = GetMostUsedAlgorithm(simulations);
        summary.BestPerformingAlgorithm = GetBestPerformingAlgorithm(simulations);

        // Add node statistics if requested
        if (request.IncludeNodeStats)
        {
            summary.NodeStats = await GetAllNodeStatsAsync(simulations);
        }

        // Add algorithm performance
        summary.AlgorithmPerformance = (await GetAlgorithmPerformanceAsync())
            .ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);

        // Add time series data if requested
        if (request.IncludeTimeSeriesData)
        {
            summary.TimeSeriesData = await GetTimeSeriesDataAsync(
                summary.StartDate, 
                summary.EndDate, 
                request.TimeBucketMinutes);
        }

        return summary;
    }

    public async Task<Dictionary<string, NodeWinnerStats>> GetWinnerDistributionAsync(Guid simulationId)
    {
        _logger.LogInformation("Getting winner distribution for simulation {SimulationId}", simulationId);
        
        var rounds = await _consensusRoundRepository.GetBySimulationRunAsync(simulationId);
        var nodes = await _nodeRepository.GetBySimulationRunAsync(simulationId);
        var blocks = await _blockRepository.GetBySimulationRunAsync(simulationId);
        
        var nodeStats = new Dictionary<string, NodeWinnerStats>();
        
        foreach (var node in nodes)
        {
            var nodeBlocks = blocks.Where(b => b.ProposerId == node.Id).ToList();
            
            var stats = new NodeWinnerStats
            {
                NodeId = node.Id.ToString(),
                BlocksProduced = nodeBlocks.Count,
                WinRate = blocks.Any() ? (double)nodeBlocks.Count / blocks.Count() * 100 : 0,
                AverageBlockTime = nodeBlocks.Any() ? 
                    nodeBlocks.Average(b => (b.Timestamp - (nodeBlocks.OrderBy(x => x.Timestamp).FirstOrDefault()?.Timestamp ?? b.Timestamp)).TotalSeconds) : 0,
                SimulationsParticipated = 1,
                AverageBlocksPerSimulation = nodeBlocks.Count,
                TotalUptime = (await GetNodeUptimeAsync(node.Id, simulationId)).TotalSeconds,
                PerformanceRank = 0, // Will be calculated later
                EfficiencyScore = nodeBlocks.Any() ? nodeBlocks.Count / Math.Max(1, (await GetNodeUptimeAsync(node.Id, simulationId)).TotalHours) : 0
            };
            
            // Calculate timing metrics for blocks
            if (nodeBlocks.Any())
            {
                var blockTimes = nodeBlocks.Select(b => 
                    (b.Timestamp - (blocks.OrderBy(x => x.Timestamp).FirstOrDefault()?.Timestamp ?? b.Timestamp)).TotalSeconds)
                    .Where(t => t > 0).ToList();
                
                stats.FastestBlockTime = blockTimes.Any() ? blockTimes.Min() : 0;
                stats.SlowestBlockTime = blockTimes.Any() ? blockTimes.Max() : 0;
            }
            
            nodeStats[node.Name] = stats;
        }
        
        // Calculate performance rankings
        var sortedStats = nodeStats.Values.OrderByDescending(s => s.WinRate).ToList();
        for (int i = 0; i < sortedStats.Count; i++)
        {
            sortedStats[i].PerformanceRank = i + 1;
        }
        
        return nodeStats;
    }

    public async Task<Dictionary<ConsensusAlgorithm, AlgorithmPerformanceMetrics>> GetAlgorithmPerformanceAsync()
    {
        _logger.LogInformation("Getting algorithm performance metrics");
        
        var allSimulations = await _simulationRunRepository.GetAllAsync();
        var algorithmGroups = allSimulations.GroupBy(s => s.ConsensusAlgorithm);
        
        var performanceMetrics = new Dictionary<ConsensusAlgorithm, AlgorithmPerformanceMetrics>();
        
        foreach (var group in algorithmGroups)
        {
            var sims = group.ToList();
            
            var metrics = new AlgorithmPerformanceMetrics
            {
                AlgorithmName = group.Key.ToString(),
                TotalSimulations = sims.Count,
                AverageBlocksPerSimulation = await GetAverageBlocksForSimulations(sims),
                AverageSimulationDuration = sims.Where(s => s.CompletedAt.HasValue && s.StartedAt.HasValue)
                    .Average(s => (s.CompletedAt!.Value - s.StartedAt!.Value).TotalSeconds),
                AverageBlockRate = await GetAverageBlockRate(sims),
                AverageTransactionThroughput = await GetAverageThroughput(sims),
                SuccessRate = sims.Any() ? (double)sims.Count(s => s.Status == SimulationStatus.Completed) / sims.Count * 100 : 0,
                TotalComputingTime = sims.Where(s => s.CompletedAt.HasValue && s.StartedAt.HasValue)
                    .Sum(s => (s.CompletedAt!.Value - s.StartedAt!.Value).TotalSeconds),
                AverageNodesPerSimulation = await GetAverageNodesForSimulations(sims),
                NetworkEfficiency = await GetNetworkEfficiency(sims),
                StabilityScore = await GetStabilityScore(sims)
            };
            
            performanceMetrics[group.Key] = metrics;
        }
        
        return performanceMetrics;
    }

    public async Task<List<TimeSeriesDataPoint>> GetTimeSeriesDataAsync(DateTime startDate, DateTime endDate, int bucketMinutes = 30)
    {
        _logger.LogInformation("Getting time-series data from {StartDate} to {EndDate} with {BucketMinutes} minute buckets",
            startDate, endDate, bucketMinutes);
        
        var simulations = await GetSimulationsByDateRangeAsync(startDate, endDate);
        var bucketSize = TimeSpan.FromMinutes(bucketMinutes);
        var dataPoints = new List<TimeSeriesDataPoint>();
        
        var currentTime = startDate;
        while (currentTime <= endDate)
        {
            var bucketEnd = currentTime.Add(bucketSize);
            var bucketSims = simulations.Where(s => s.StartedAt >= currentTime && s.StartedAt < bucketEnd).ToList();
            
            var dataPoint = new TimeSeriesDataPoint
            {
                Timestamp = currentTime,
                ActiveSimulations = bucketSims.Count(s => s.Status == SimulationStatus.Running),
                BlocksCreated = await GetBlocksCreatedInPeriod(bucketSims),
                TransactionsProcessed = await GetTransactionsInPeriod(bucketSims),
                AverageBlockRate = await GetAverageBlockRateForPeriod(bucketSims),
                ActiveNodes = await GetActiveNodesInPeriod(bucketSims),
                AlgorithmDistribution = GetAlgorithmDistributionForPeriod(bucketSims)
            };
            
            dataPoints.Add(dataPoint);
            currentTime = bucketEnd;
        }
        
        return dataPoints;
    }

    public async Task<byte[]> ExportAnalyticsAsync(AnalyticsRequest request, string format)
    {
        _logger.LogInformation("Exporting analytics data in {Format} format", format);
        
        var summary = await GenerateAnalyticsSummaryAsync(request);
        
        return format.ToLower() switch
        {
            "csv" => ExportToCsv(summary),
            "json" => ExportToJson(summary),
            _ => throw new ArgumentException($"Unsupported export format: {format}")
        };
    }

    // Helper methods
    private async Task<IEnumerable<SimulationRun>> GetFilteredSimulationsAsync(AnalyticsRequest request)
    {
        var allSims = await _simulationRunRepository.GetAllAsync();
        
        if (request.StartDate.HasValue || request.EndDate.HasValue)
        {
            allSims = allSims.Where(s => 
                (!request.StartDate.HasValue || s.StartedAt >= request.StartDate.Value) &&
                (!request.EndDate.HasValue || s.StartedAt <= request.EndDate.Value));
        }
        
        if (request.MinSimulationDuration.HasValue)
        {
            allSims = allSims.Where(s => s.CompletedAt.HasValue && s.StartedAt.HasValue && 
                (s.CompletedAt.Value - s.StartedAt.Value).TotalSeconds >= request.MinSimulationDuration.Value);
        }
        
        return allSims.ToList();
    }

    private async Task<Dictionary<string, NodeWinnerStats>> GetAllNodeStatsAsync(IEnumerable<SimulationRun> simulations)
    {
        var allStats = new Dictionary<string, NodeWinnerStats>();
        
        foreach (var sim in simulations)
        {
            var simStats = await GetWinnerDistributionAsync(sim.Id);
            foreach (var kvp in simStats)
            {
                if (allStats.ContainsKey(kvp.Key))
                {
                    var existing = allStats[kvp.Key];
                    existing.BlocksProduced += kvp.Value.BlocksProduced;
                    existing.SimulationsParticipated += 1;
                    // Recalculate win rate
                    existing.WinRate = allStats.Values.Sum(s => s.BlocksProduced) > 0 ? 
                        (double)existing.BlocksProduced / allStats.Values.Sum(s => s.BlocksProduced) * 100 : 0;
                }
                else
                {
                    allStats[kvp.Key] = kvp.Value;
                }
            }
        }
        
        return allStats;
    }

    private async Task<long> CountBlocksBySimulationsAsync(IEnumerable<SimulationRun> simulations)
    {
        long totalBlocks = 0;
        
        foreach (var sim in simulations)
        {
            var blocks = await _blockRepository.GetBySimulationRunAsync(sim.Id);
            totalBlocks += blocks.Count();
        }
        
        return totalBlocks;
    }

    private Dictionary<string, int> GetStatusDistribution(IEnumerable<SimulationRun> simulations)
    {
        return simulations
            .GroupBy(s => s.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private string? GetMostUsedAlgorithm(IEnumerable<SimulationRun> simulations)
    {
        return simulations
            .GroupBy(s => s.ConsensusAlgorithm.ToString())
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;
    }

    private string? GetBestPerformingAlgorithm(IEnumerable<SimulationRun> simulations)
    {
        // Simple heuristic: algorithm with highest completion rate
        return simulations
            .GroupBy(s => s.ConsensusAlgorithm.ToString())
            .Where(g => g.Any())
            .OrderByDescending(g => (double)g.Count(s => s.Status == SimulationStatus.Completed) / g.Count())
            .FirstOrDefault()?.Key;
    }

    // Placeholder implementations for detailed calculations
    private async Task<TimeSpan> GetNodeUptimeAsync(Guid nodeId, Guid simulationId)
    {
        // Placeholder - would calculate from event logs or node status changes
        return TimeSpan.FromHours(1);
    }

    private async Task<IEnumerable<SimulationRun>> GetSimulationsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var allSims = await _simulationRunRepository.GetAllAsync();
        return allSims.Where(s => s.StartedAt >= startDate && s.StartedAt <= endDate);
    }

    private async Task<double> GetAverageBlocksForSimulations(IEnumerable<SimulationRun> simulations)
    {
        if (!simulations.Any()) return 0;
        
        var totalBlocks = await CountBlocksBySimulationsAsync(simulations);
        return (double)totalBlocks / simulations.Count();
    }

    private async Task<double> GetAverageBlockRate(IEnumerable<SimulationRun> simulations)
    {
        // Blocks per second
        var totalBlocks = await CountBlocksBySimulationsAsync(simulations);
        var totalSeconds = simulations.Where(s => s.CompletedAt.HasValue && s.StartedAt.HasValue)
            .Sum(s => (s.CompletedAt!.Value - s.StartedAt!.Value).TotalSeconds);
        
        return totalSeconds > 0 ? totalBlocks / totalSeconds : 0;
    }

    private async Task<double> GetAverageThroughput(IEnumerable<SimulationRun> simulations)
    {
        // Placeholder for transactions per second
        return 10.5;
    }

    private async Task<double> GetAverageNodesForSimulations(IEnumerable<SimulationRun> simulations)
    {
        if (!simulations.Any()) return 0;
        
        var totalNodes = 0;
        foreach (var sim in simulations)
        {
            var nodes = await _nodeRepository.GetBySimulationRunAsync(sim.Id);
            totalNodes += nodes.Count();
        }
        
        return (double)totalNodes / simulations.Count();
    }

    private async Task<double> GetNetworkEfficiency(IEnumerable<SimulationRun> simulations)
    {
        var avgThroughput = await GetAverageThroughput(simulations);
        var avgNodes = await GetAverageNodesForSimulations(simulations);
        
        return avgNodes > 0 ? avgThroughput / avgNodes : 0;
    }

    private async Task<double> GetStabilityScore(IEnumerable<SimulationRun> simulations)
    {
        // Placeholder - would calculate variance in performance metrics
        return 85.0;
    }

    private async Task<int> GetBlocksCreatedInPeriod(IEnumerable<SimulationRun> simulations)
    {
        return (int)await CountBlocksBySimulationsAsync(simulations);
    }

    private async Task<int> GetTransactionsInPeriod(IEnumerable<SimulationRun> simulations)
    {
        // Placeholder - would count transactions
        return simulations.Count() * 50;
    }

    private async Task<double> GetAverageBlockRateForPeriod(IEnumerable<SimulationRun> simulations)
    {
        return await GetAverageBlockRate(simulations);
    }

    private async Task<int> GetActiveNodesInPeriod(IEnumerable<SimulationRun> simulations)
    {
        var totalNodes = 0;
        foreach (var sim in simulations)
        {
            var nodes = await _nodeRepository.GetBySimulationRunAsync(sim.Id);
            totalNodes += nodes.Count();
        }
        
        return totalNodes;
    }

    private Dictionary<string, int> GetAlgorithmDistributionForPeriod(IEnumerable<SimulationRun> simulations)
    {
        return simulations
            .GroupBy(s => s.ConsensusAlgorithm.ToString())
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private byte[] ExportToCsv(AnalyticsSummary summary)
    {
        var csv = $"TotalSimulations,{summary.TotalSimulations}\n";
        csv += $"TotalBlocks,{summary.TotalBlocks}\n";
        csv += $"AverageBlocksPerSimulation,{summary.AverageBlocksPerSimulation}\n";
        csv += $"AverageSimulationDuration,{summary.AverageSimulationDuration}\n";
        
        if (summary.NodeStats?.Any() == true)
        {
            csv += "\nNode Statistics:\n";
            csv += "NodeId,BlocksProduced,WinRate\n";
            foreach (var kvp in summary.NodeStats)
            {
                csv += $"{kvp.Key},{kvp.Value.BlocksProduced},{kvp.Value.WinRate:F2}\n";
            }
        }
        
        return System.Text.Encoding.UTF8.GetBytes(csv);
    }

    private byte[] ExportToJson(AnalyticsSummary summary)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(summary, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        return System.Text.Encoding.UTF8.GetBytes(json);
    }
}