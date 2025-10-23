namespace Consensus.Web.Exceptions;

/// <summary>
/// Exception thrown when simulation operations fail
/// </summary>
public class SimulationException : Exception
{
    public string? SimulationId { get; }
    public string? OperationType { get; }
    public Dictionary<string, object>? Context { get; }

    public SimulationException(string message) : base(message)
    {
    }

    public SimulationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public SimulationException(string message, string simulationId, string operationType) : base(message)
    {
        SimulationId = simulationId;
        OperationType = operationType;
    }

    public SimulationException(string message, string simulationId, string operationType, Dictionary<string, object> context) : base(message)
    {
        SimulationId = simulationId;
        OperationType = operationType;
        Context = context;
    }

    public SimulationException(string message, Exception innerException, string simulationId, string operationType) : base(message, innerException)
    {
        SimulationId = simulationId;
        OperationType = operationType;
    }
}

/// <summary>
/// Exception thrown when consensus protocol operations fail
/// </summary>
public class ConsensusException : Exception
{
    public string? Protocol { get; }
    public int RoundNumber { get; }
    public string? NodeId { get; }

    public ConsensusException(string message) : base(message)
    {
    }

    public ConsensusException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ConsensusException(string message, string protocol, int roundNumber) : base(message)
    {
        Protocol = protocol;
        RoundNumber = roundNumber;
    }

    public ConsensusException(string message, string protocol, int roundNumber, string nodeId) : base(message)
    {
        Protocol = protocol;
        RoundNumber = roundNumber;
        NodeId = nodeId;
    }

    public ConsensusException(string message, Exception innerException, string protocol, int roundNumber) : base(message, innerException)
    {
        Protocol = protocol;
        RoundNumber = roundNumber;
    }
}

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : Exception
{
    public IEnumerable<string> ValidationErrors { get; }

    public ValidationException(string message, IEnumerable<string> validationErrors) : base(message)
    {
        ValidationErrors = validationErrors ?? Array.Empty<string>();
    }

    public ValidationException(IEnumerable<string> validationErrors) : base("Validation failed")
    {
        ValidationErrors = validationErrors ?? Array.Empty<string>();
    }
}