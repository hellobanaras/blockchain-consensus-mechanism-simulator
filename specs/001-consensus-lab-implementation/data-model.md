# Data Model: Consensus Lab

## Core Entities

### Node
Represents a virtual participant in the consensus simulation.

**Fields:**
- `Id` (Guid): Primary key
- `DisplayName` (string): Human-readable identifier (e.g., "N0", "N1")
- `ExperimentId` (Guid): Foreign key to Experiment
- `Role` (string): Node role for PoA ("Authority", "Participant") 
- `Power` (decimal): Hash power for PoW protocols
- `Stake` (decimal): Token stake for PoS protocols
- `Burned` (decimal): Cumulative tokens burned in PoB
- `Votes` (int): Vote count for DPoS delegate selection
- `IsHonest` (bool): Fault injection flag for PBFT
- `CreatedAt` (DateTime): Timestamp

**Relationships:**
- Many-to-One: Experiment
- One-to-Many: Block (as Proposer)
- One-to-Many: EventLog (as Actor)

**Validation Rules:**
- DisplayName: Required, max 50 characters
- Power: >= 0, <= 1000
- Stake: >= 0, <= 10000
- Votes: >= 0

### Round  
Represents a single consensus iteration that produces a block.

**Fields:**
- `Id` (Guid): Primary key
- `ExperimentId` (Guid): Foreign key to Experiment
- `Index` (int): Sequential round number (0-based)
- `Protocol` (string): Protocol used ("pow", "pos", "dpos", "poa", "pbft", "poet", "pob")
- `StartedAt` (DateTime): Round start timestamp
- `EndedAt` (DateTime?): Round completion timestamp
- `ParamsJson` (string): Protocol-specific parameters as JSON
- `WinnerNodeId` (Guid?): Foreign key to winning Node
- `Status` (string): "pending", "completed", "failed"

**Relationships:**
- Many-to-One: Experiment
- Many-to-One: Node (Winner)
- One-to-Many: Block
- One-to-Many: EventLog

**Validation Rules:**
- Index: >= 0, unique within experiment
- Protocol: Must be valid enum value
- EndedAt: Must be > StartedAt if set

**State Transitions:**
- pending -> completed (normal flow)
- pending -> failed (error occurred)
- No transitions from completed/failed states

### Block
Immutable unit representing consensus result with hash chain linkage.

**Fields:**
- `Id` (Guid): Primary key  
- `Height` (int): Block number in chain (0-based)
- `Hash` (string): Block hash (SHA256 hex)
- `PrevHash` (string): Previous block hash for chain linkage
- `ProposerNodeId` (Guid): Foreign key to proposing Node
- `RoundId` (Guid): Foreign key to Round that created this block
- `Protocol` (string): Consensus protocol used
- `PayloadJson` (string?): Optional payload data (Supply-Chain/FL)
- `MetaJson` (string): Protocol-specific metadata (wait times, votes, etc.)
- `Timestamp` (DateTime): Block creation time
- `ExperimentId` (Guid): Foreign key to Experiment (denormalized for queries)

**Relationships:**
- Many-to-One: Experiment
- Many-to-One: Round  
- Many-to-One: Node (Proposer)

**Validation Rules:**
- Height: >= 0, sequential within experiment
- Hash: Required, 64 character hex string
- PrevHash: Required for height > 0, null for genesis
- Timestamp: <= current time

### Experiment
Complete simulation run with configuration and results.

**Fields:**
- `Id` (Guid): Primary key
- `Name` (string): User-defined experiment name
- `Protocol` (string): Primary protocol for simulation
- `NodeCount` (int): Number of nodes in simulation
- `Rounds` (int): Target number of rounds to run
- `ParamsJson` (string): Global protocol parameters as JSON
- `Status` (string): "pending", "running", "completed", "failed", "paused"
- `StartedAt` (DateTime?): Simulation start time
- `CompletedAt` (DateTime?): Simulation completion time  
- `FinalHeight` (int?): Final blockchain height achieved
- `CreatedBy` (string): User identifier who created experiment
- `CreatedAt` (DateTime): Entity creation timestamp

**Relationships:**
- One-to-Many: Node
- One-to-Many: Round
- One-to-Many: Block
- One-to-Many: EventLog

**Validation Rules:**
- Name: Required, max 100 characters
- NodeCount: 5-100
- Rounds: 1-100  
- Protocol: Must be valid enum value
- CreatedBy: Required

**State Transitions:**
- pending -> running (simulation started)
- running -> paused (user paused)
- running -> completed (all rounds finished)
- running -> failed (error occurred)
- paused -> running (user resumed)
- paused -> completed (if rounds completed while paused)

### EventLog
Audit trail and debugging information for simulation events.

**Fields:**
- `Id` (Guid): Primary key
- `ExperimentId` (Guid): Foreign key to Experiment
- `RoundId` (Guid?): Foreign key to Round (null for experiment-level events)
- `Type` (string): Event category ("round_start", "round_end", "block_created", "error")
- `Message` (string): Human-readable event description
- `DataJson` (string?): Structured event data as JSON
- `Timestamp` (DateTime): Event occurrence time

**Relationships:**
- Many-to-One: Experiment
- Many-to-One: Round (optional)

**Validation Rules:**
- Type: Required, must be valid enum value
- Message: Required, max 500 characters
- Timestamp: Required, <= current time

## DTOs and View Models

### StartSimulationRequest
```csharp
{
    "protocol": "poet",
    "nodeCount": 6,
    "rounds": 20,
    "params": {
        "waitLowMs": 50,
        "waitHighMs": 300,
        "seed": 7
    }
}
```

### RoundUpdate (SignalR)
```csharp  
{
    "round": 5,
    "winnerNodeId": "guid",
    "blockHash": "abc123...",
    "stats": {
        "waitMs": 124,
        "votes": 5,
        "totalBurn": 15.5
    }
}
```

### AnalyticsSummary
```csharp
{
    "height": 20,
    "winnerDistribution": {
        "N0": 3,
        "N1": 4,
        "N2": 2
    },
    "protocolMetrics": {
        "avgWaitMs": 124.5,
        "quorum": 5,
        "forks": 0,
        "totalBurned": 150.0
    }
}
```

## Database Schema Considerations

### Indexes
- `Experiment.CreatedBy` - for user experiment queries
- `Block.ExperimentId, Block.Height` - for blockchain queries
- `Round.ExperimentId, Round.Index` - for round timeline queries  
- `EventLog.ExperimentId, EventLog.Timestamp` - for audit queries

### JSON Columns
- Protocol parameters stored as JSON for flexibility
- Payload data as JSON for Supply-Chain/FL modes
- Metadata as JSON for protocol-specific data (votes, wait times, etc.)
- EventLog data as JSON for structured logging

### Constraints
- `Block.Height` unique within experiment
- `Round.Index` unique within experiment
- `Node.DisplayName` unique within experiment
- Foreign key constraints with cascade delete on experiment