using System.ComponentModel.DataAnnotations;
using Consensus.Core.Enums;

namespace Consensus.Core.Entities;

/// <summary>
/// Represents the network topology configuration for a simulation
/// </summary>
public class NetworkTopology
{
    /// <summary>
    /// Unique identifier for the network topology
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the network topology
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the topology
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Type of network topology
    /// </summary>
    [Required]
    public NetworkTopologyType TopologyType { get; set; }

    /// <summary>
    /// Number of nodes in the topology
    /// </summary>
    public int NodeCount { get; set; }

    /// <summary>
    /// Average number of connections per node
    /// </summary>
    public double AverageConnections { get; set; }

    /// <summary>
    /// Maximum latency in the network (milliseconds)
    /// </summary>
    public int MaxLatencyMs { get; set; }

    /// <summary>
    /// Minimum latency in the network (milliseconds)
    /// </summary>
    public int MinLatencyMs { get; set; }

    /// <summary>
    /// Network partition probability (for fault injection)
    /// </summary>
    public double PartitionProbability { get; set; }

    /// <summary>
    /// Message loss probability (for unreliable networks)
    /// </summary>
    public double MessageLossProbability { get; set; }

    /// <summary>
    /// Bandwidth limit in bytes per second (0 = unlimited)
    /// </summary>
    public long BandwidthLimitBps { get; set; }

    /// <summary>
    /// Configuration specific to the topology type
    /// </summary>
    public Dictionary<string, object>? Configuration { get; set; }

    /// <summary>
    /// Adjacency matrix representing connections between nodes
    /// Stored as JSON in the database
    /// </summary>
    public int[,]? AdjacencyMatrix { get; set; }

    /// <summary>
    /// List of network partitions (if any)
    /// </summary>
    public List<List<int>>? Partitions { get; set; }

    /// <summary>
    /// Simulation run this topology belongs to
    /// </summary>
    [Required]
    public Guid SimulationRunId { get; set; }

    /// <summary>
    /// When the topology was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the topology was last modified
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual SimulationRun? SimulationRun { get; set; }

    /// <summary>
    /// Generates the network topology based on the type
    /// </summary>
    public void GenerateTopology(int nodeCount, Random? random = null)
    {
        NodeCount = nodeCount;
        random ??= new Random();
        
        AdjacencyMatrix = TopologyType switch
        {
            NetworkTopologyType.FullMesh => GenerateFullMesh(nodeCount),
            NetworkTopologyType.Ring => GenerateRing(nodeCount),
            NetworkTopologyType.Star => GenerateStar(nodeCount),
            NetworkTopologyType.Tree => GenerateTree(nodeCount, random),
            NetworkTopologyType.Random => GenerateRandom(nodeCount, random),
            NetworkTopologyType.SmallWorld => GenerateSmallWorld(nodeCount, random),
            NetworkTopologyType.ScaleFree => GenerateScaleFree(nodeCount, random),
            NetworkTopologyType.Grid => GenerateGrid(nodeCount),
            NetworkTopologyType.Custom => GenerateCustom(nodeCount),
            _ => GenerateFullMesh(nodeCount)
        };
        
        CalculateMetrics();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Generates a full mesh topology where every node is connected to every other node
    /// </summary>
    private int[,] GenerateFullMesh(int nodeCount)
    {
        var matrix = new int[nodeCount, nodeCount];
        
        for (int i = 0; i < nodeCount; i++)
        {
            for (int j = 0; j < nodeCount; j++)
            {
                matrix[i, j] = i != j ? 1 : 0; // Connected to all except self
            }
        }
        
        return matrix;
    }

    /// <summary>
    /// Generates a ring topology where nodes form a circular connection
    /// </summary>
    private int[,] GenerateRing(int nodeCount)
    {
        var matrix = new int[nodeCount, nodeCount];
        
        for (int i = 0; i < nodeCount; i++)
        {
            matrix[i, (i + 1) % nodeCount] = 1; // Connect to next node
            matrix[(i + 1) % nodeCount, i] = 1; // Bidirectional
        }
        
        return matrix;
    }

    /// <summary>
    /// Generates a star topology with one central hub
    /// </summary>
    private int[,] GenerateStar(int nodeCount)
    {
        var matrix = new int[nodeCount, nodeCount];
        
        if (nodeCount > 1)
        {
            for (int i = 1; i < nodeCount; i++)
            {
                matrix[0, i] = 1; // Hub to spoke
                matrix[i, 0] = 1; // Spoke to hub
            }
        }
        
        return matrix;
    }

    /// <summary>
    /// Generates a random topology
    /// </summary>
    private int[,] GenerateRandom(int nodeCount, Random random)
    {
        var matrix = new int[nodeCount, nodeCount];
        double connectionProbability = Configuration?.ContainsKey("connectionProbability") == true 
            ? Convert.ToDouble(Configuration["connectionProbability"]) 
            : 0.3; // Default 30% connection probability
        
        for (int i = 0; i < nodeCount; i++)
        {
            for (int j = i + 1; j < nodeCount; j++)
            {
                if (random.NextDouble() < connectionProbability)
                {
                    matrix[i, j] = 1;
                    matrix[j, i] = 1; // Bidirectional
                }
            }
        }
        
        return matrix;
    }

    /// <summary>
    /// Generates other topology types (simplified implementations)
    /// </summary>
    private int[,] GenerateTree(int nodeCount, Random random) => GenerateRandom(nodeCount, random);
    private int[,] GenerateSmallWorld(int nodeCount, Random random) => GenerateRandom(nodeCount, random);
    private int[,] GenerateScaleFree(int nodeCount, Random random) => GenerateRandom(nodeCount, random);
    private int[,] GenerateGrid(int nodeCount) => GenerateRing(nodeCount); // Simplified
    private int[,] GenerateCustom(int nodeCount) => new int[nodeCount, nodeCount]; // Empty for custom configuration

    /// <summary>
    /// Calculates network metrics based on the adjacency matrix
    /// </summary>
    private void CalculateMetrics()
    {
        if (AdjacencyMatrix == null) return;
        
        int totalConnections = 0;
        
        for (int i = 0; i < NodeCount; i++)
        {
            for (int j = 0; j < NodeCount; j++)
            {
                totalConnections += AdjacencyMatrix[i, j];
            }
        }
        
        AverageConnections = NodeCount > 0 ? (double)totalConnections / NodeCount : 0;
    }

    /// <summary>
    /// Checks if two nodes are directly connected
    /// </summary>
    public bool AreNodesConnected(int nodeA, int nodeB)
    {
        if (AdjacencyMatrix == null) return false;
        if (nodeA < 0 || nodeA >= NodeCount) return false;
        if (nodeB < 0 || nodeB >= NodeCount) return false;
        
        return AdjacencyMatrix[nodeA, nodeB] == 1;
    }

    /// <summary>
    /// Gets the latency between two nodes
    /// </summary>
    public int GetLatency(int nodeA, int nodeB, Random? random = null)
    {
        if (!AreNodesConnected(nodeA, nodeB)) return int.MaxValue; // No connection
        
        random ??= new Random();
        
        // Return random latency within the specified range
        return random.Next(MinLatencyMs, MaxLatencyMs + 1);
    }

    /// <summary>
    /// Simulates a network partition
    /// </summary>
    public void CreatePartition(List<int> partition1, List<int> partition2)
    {
        if (AdjacencyMatrix == null) return;
        
        // Remove connections between the two partitions
        foreach (var node1 in partition1)
        {
            foreach (var node2 in partition2)
            {
                if (node1 < NodeCount && node2 < NodeCount)
                {
                    AdjacencyMatrix[node1, node2] = 0;
                    AdjacencyMatrix[node2, node1] = 0;
                }
            }
        }
        
        Partitions ??= new List<List<int>>();
        Partitions.Add(partition1);
        Partitions.Add(partition2);
        
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Heals network partitions
    /// </summary>
    public void HealPartitions()
    {
        if (Partitions != null)
        {
            Partitions.Clear();
            GenerateTopology(NodeCount); // Regenerate original topology
        }
    }

    /// <summary>
    /// Gets the degree (number of connections) of a specific node
    /// </summary>
    public int GetNodeDegree(int nodeId)
    {
        if (AdjacencyMatrix == null || nodeId < 0 || nodeId >= NodeCount) return 0;
        
        int degree = 0;
        for (int i = 0; i < NodeCount; i++)
        {
            degree += AdjacencyMatrix[nodeId, i];
        }
        
        return degree;
    }

    public override string ToString()
    {
        return $"NetworkTopology {Name} - Type: {TopologyType}, Nodes: {NodeCount}, Avg Connections: {AverageConnections:F1}";
    }
}