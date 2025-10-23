using Microsoft.Extensions.Logging;
using Consensus.Core.Protocols;
using Consensus.Core.Entities;
using Consensus.Core.Enums;

namespace Consensus.Core.Examples;

/// <summary>
/// Demonstration of PoW consensus protocol functionality
/// This shows how to initialize, configure, and run PoW mining simulations
/// </summary>
public class PowDemo
{
    public static async Task RunDemoAsync()
    {
        Console.WriteLine("🔨 Proof of Work (PoW) Protocol Demonstration");
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine();

        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger<PowProtocol>();

        // Create PoW protocol instance
        var powProtocol = new PowProtocol(logger);

        // Display protocol information
        Console.WriteLine($"📋 Protocol Information:");
        Console.WriteLine($"   Name: {powProtocol.Name}");
        Console.WriteLine($"   Algorithm: {powProtocol.Algorithm}");
        Console.WriteLine($"   Description: {powProtocol.Description}");
        Console.WriteLine($"   Minimum Nodes: {powProtocol.MinimumNodes}");
        Console.WriteLine($"   Byzantine Fault Tolerance: {powProtocol.SupportsByzantineFaultTolerance}");
        Console.WriteLine();

        // Create test nodes with varying computational power
        var nodes = new List<Node>
        {
            new Node
            {
                Id = Guid.NewGuid(),
                Name = "Mining-Node-1",
                IsActive = true,
                Status = NodeStatus.Online,
                ConsensusAlgorithm = ConsensusAlgorithm.ProofOfWork,
                ComputationalPower = 1500, // High-end miner
                SimulationRunId = Guid.NewGuid()
            },
            new Node
            {
                Id = Guid.NewGuid(),
                Name = "Mining-Node-2",
                IsActive = true,
                Status = NodeStatus.Online,
                ConsensusAlgorithm = ConsensusAlgorithm.ProofOfWork,
                ComputationalPower = 1000, // Medium miner
                SimulationRunId = Guid.NewGuid()
            },
            new Node
            {
                Id = Guid.NewGuid(),
                Name = "Mining-Node-3",
                IsActive = true,
                Status = NodeStatus.Online,
                ConsensusAlgorithm = ConsensusAlgorithm.ProofOfWork,
                ComputationalPower = 800, // Lower-end miner
                SimulationRunId = Guid.NewGuid()
            }
        };

        Console.WriteLine($"⚙️  Created {nodes.Count} mining nodes:");
        foreach (var node in nodes)
        {
            Console.WriteLine($"   • {node.Name}: {node.ComputationalPower} H/s computational power");
        }
        Console.WriteLine();

        // Configure PoW protocol with custom parameters
        var configuration = new Dictionary<string, object>
        {
            ["difficulty"] = 3, // Moderate difficulty for demo
            ["maxHashAttemptsPerNode"] = 10000,
            ["blockTimeTargetMs"] = 3000, // 3 second target
            ["timeoutMs"] = 15000 // 15 second timeout
        };

        Console.WriteLine($"🔧 Protocol Configuration:");
        foreach (var kvp in configuration)
        {
            Console.WriteLine($"   {kvp.Key}: {kvp.Value}");
        }
        Console.WriteLine();

        // Initialize the protocol
        Console.WriteLine("🚀 Initializing PoW protocol...");
        await powProtocol.InitializeAsync(nodes, configuration);
        Console.WriteLine($"✅ Initialized with {powProtocol.ParticipatingNodes.Count} participating miners");
        Console.WriteLine();

        // Create genesis block
        Console.WriteLine("🧱 Creating genesis block...");
        var simulationRunId = Guid.NewGuid();
        var genesisBlock = await powProtocol.CreateGenesisBlockAsync(simulationRunId);
        Console.WriteLine($"✅ Genesis block created: {genesisBlock.Hash}");
        Console.WriteLine();

        // Run multiple mining rounds
        Console.WriteLine("⛏️  Starting mining rounds...");
        var roundResults = new List<(int round, bool success, string? winner, double timeMs)>();

        for (int i = 1; i <= 5; i++)
        {
            Console.WriteLine($"\n🏁 Round {i}:");
            
            var round = await powProtocol.PrepareRoundAsync(i, simulationRunId);
            Console.WriteLine($"   Prepared round with {round.ParticipatingNodes} miners");

            var startTime = DateTime.UtcNow;
            var result = await powProtocol.ExecuteRoundAsync(round, CancellationToken.None);
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            if (result.Success)
            {
                var winnerNode = nodes.FirstOrDefault(n => n.Id.ToString() == result.LeaderId);
                Console.WriteLine($"   ✅ Mining successful!");
                Console.WriteLine($"   🏆 Winner: {winnerNode?.Name ?? "Unknown"} ({result.LeaderId})");
                Console.WriteLine($"   ⏱️  Time: {duration:F0}ms");
                
                if (result.Metrics != null && result.Metrics.ContainsKey("nonce"))
                {
                    Console.WriteLine($"   🔢 Nonce: {result.Metrics["nonce"]}");
                }
                if (result.Metrics != null && result.Metrics.ContainsKey("hash"))
                {
                    Console.WriteLine($"   🔗 Hash: {result.Metrics["hash"]}");
                }

                roundResults.Add((i, true, winnerNode?.Name, duration));
            }
            else
            {
                Console.WriteLine($"   ❌ Mining failed: {result.ErrorMessage}");
                Console.WriteLine($"   ⏱️  Time: {duration:F0}ms");
                roundResults.Add((i, false, null, duration));
            }
        }

        // Display overall statistics
        Console.WriteLine("\n📊 Mining Statistics:");
        Console.WriteLine("=".PadRight(40, '='));
        
        var successfulRounds = roundResults.Count(r => r.success);
        var averageTime = roundResults.Where(r => r.success).Average(r => r.timeMs);
        
        Console.WriteLine($"Total Rounds: {roundResults.Count}");
        Console.WriteLine($"Successful: {successfulRounds}");
        Console.WriteLine($"Success Rate: {(double)successfulRounds / roundResults.Count:P1}");
        Console.WriteLine($"Average Mining Time: {averageTime:F0}ms");
        Console.WriteLine();

        // Display winner distribution
        var winnerCounts = roundResults
            .Where(r => r.success && r.winner != null)
            .GroupBy(r => r.winner)
            .ToDictionary(g => g.Key!, g => g.Count());

        if (winnerCounts.Any())
        {
            Console.WriteLine("🏆 Winner Distribution:");
            foreach (var kvp in winnerCounts.OrderByDescending(x => x.Value))
            {
                var percentage = (double)kvp.Value / successfulRounds * 100;
                Console.WriteLine($"   {kvp.Key}: {kvp.Value} blocks ({percentage:F1}%)");
            }
            Console.WriteLine();
        }

        // Get protocol metrics
        var metrics = powProtocol.GetMetrics();
        Console.WriteLine("📈 Protocol Metrics:");
        foreach (var kvp in metrics)
        {
            if (kvp.Value is Dictionary<Guid, int> guidDict)
            {
                Console.WriteLine($"   {kvp.Key}:");
                foreach (var item in guidDict)
                {
                    var nodeName = nodes.FirstOrDefault(n => n.Id == item.Key)?.Name ?? "Unknown";
                    Console.WriteLine($"     {nodeName}: {item.Value}");
                }
            }
            else
            {
                Console.WriteLine($"   {kvp.Key}: {kvp.Value}");
            }
        }
        Console.WriteLine();

        // Test fault tolerance
        Console.WriteLine("🔧 Testing Byzantine Fault Tolerance...");
        var byzantineNode = nodes.First();
        Console.WriteLine($"   Marking {byzantineNode.Name} as Byzantine");
        
        await powProtocol.HandleNodeFaultAsync(byzantineNode, FaultType.Byzantine);
        Console.WriteLine($"   ✅ Byzantine node handled, remaining miners: {powProtocol.ParticipatingNodes.Count}");
        Console.WriteLine();

        // Cleanup
        Console.WriteLine("🧹 Cleaning up...");
        await powProtocol.CleanupAsync();
        Console.WriteLine("✅ Demo completed successfully!");
        Console.WriteLine();
    }
}

/// <summary>
/// Console application entry point for running the PoW demo
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            await PowDemo.RunDemoAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Demo failed with error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}