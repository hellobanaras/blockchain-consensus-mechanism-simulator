# SignalR Hub Contract: Consensus Simulation

## Hub Endpoint
`/hubs/simulation`

## Connection Groups
Clients are automatically added to groups based on simulation ID:
- Group name: `simulation-{simulationId}`
- Automatic cleanup when simulation completes

## Server-to-Client Events

### RoundStarted
Broadcast when a new consensus round begins.

**Event**: `RoundStarted`
**Payload**:
```typescript
{
  round: number;           // Round index (0-based)
  experimentId: string;    // Simulation UUID
  protocol: string;        // Consensus protocol name
  params: object;          // Protocol-specific parameters
  timestamp: string;       // ISO 8601 timestamp
}
```

**Example**:
```json
{
  "round": 5,
  "experimentId": "123e4567-e89b-12d3-a456-426614174000",
  "protocol": "poet",
  "params": {
    "waitLowMs": 50,
    "waitHighMs": 300
  },
  "timestamp": "2025-10-18T10:30:00Z"
}
```

### RoundUpdate
Broadcast during round execution with winner and statistics.

**Event**: `RoundUpdate`
**Payload**:
```typescript
{
  round: number;           // Round index
  experimentId: string;    // Simulation UUID
  winner: {
    nodeId: string;        // Winner node UUID
    displayName: string;   // Winner node name (e.g., "N0")
  };
  block: {
    hash: string;          // Block hash
    height: number;        // Block height in chain
  };
  stats: {               // Protocol-specific statistics
    waitMs?: number;       // Wait time for PoET
    votes?: number;        // Vote count for PBFT/DPoS
    totalBurn?: number;    // Burn amount for PoB
    stakeAfter?: number;   // Remaining stake for PoS
    orphaned?: boolean;    // Fork flag for PoW
  };
  timestamp: string;       // ISO 8601 timestamp
}
```

**Example**:
```json
{
  "round": 5,
  "experimentId": "123e4567-e89b-12d3-a456-426614174000",
  "winner": {
    "nodeId": "456e7890-e89b-12d3-a456-426614174111",
    "displayName": "N2"
  },
  "block": {
    "hash": "abc123def456789...",
    "height": 5
  },
  "stats": {
    "waitMs": 124,
    "votes": null,
    "totalBurn": null
  },
  "timestamp": "2025-10-18T10:30:02Z"
}
```

### RoundCompleted
Broadcast when a consensus round successfully completes.

**Event**: `RoundCompleted`
**Payload**:
```typescript
{
  round: number;           // Round index
  experimentId: string;    // Simulation UUID
  height: number;          // Current blockchain height
  validations: number;     // Number of validating nodes
  duration: number;        // Round duration in milliseconds
  timestamp: string;       // ISO 8601 timestamp
}
```

### SimulationCompleted
Broadcast when entire simulation finishes.

**Event**: `SimulationCompleted`
**Payload**:
```typescript
{
  experimentId: string;    // Simulation UUID
  finalHeight: number;     // Final blockchain height
  totalRounds: number;     // Total rounds executed
  duration: number;        // Total simulation time in milliseconds
  status: string;          // "completed" | "failed" | "stopped"
  timestamp: string;       // ISO 8601 timestamp
}
```

### SimulationError
Broadcast when simulation encounters an error.

**Event**: `SimulationError`
**Payload**:
```typescript
{
  experimentId: string;    // Simulation UUID
  round?: number;          // Round where error occurred (if applicable)
  error: string;           // Error message
  code: string;            // Error code for client handling
  timestamp: string;       // ISO 8601 timestamp
}
```

## Client-to-Server Methods

### JoinSimulation
Join a specific simulation group to receive updates.

**Method**: `JoinSimulation`
**Parameters**:
```typescript
{
  simulationId: string;    // Simulation UUID to join
}
```

**Response**: `boolean` (success/failure)

### LeaveSimulation
Leave a simulation group to stop receiving updates.

**Method**: `LeaveSimulation`
**Parameters**:
```typescript
{
  simulationId: string;    // Simulation UUID to leave
}
```

**Response**: `boolean` (success/failure)

### PauseSimulation
Request to pause a running simulation (Operator+ role required).

**Method**: `PauseSimulation`
**Parameters**:
```typescript
{
  simulationId: string;    // Simulation UUID to pause
}
```

**Response**: 
```typescript
{
  success: boolean;
  message?: string;        // Error message if failed
}
```

### ResumeSimulation
Request to resume a paused simulation (Operator+ role required).

**Method**: `ResumeSimulation`
**Parameters**:
```typescript
{
  simulationId: string;    // Simulation UUID to resume
}
```

**Response**:
```typescript
{
  success: boolean;
  message?: string;        // Error message if failed
}
```

## Connection Events

### OnConnectedAsync
Server method called when client connects.
- Adds connection to user-specific group
- Logs connection for audit trail

### OnDisconnectedAsync
Server method called when client disconnects.
- Removes connection from all groups
- Cleans up user-specific resources

## Authentication & Authorization

### Connection Requirements
- Valid authentication cookie or JWT token
- User must have Viewer role or higher
- Rate limiting: max 10 connections per user

### Method Authorization
- `JoinSimulation`/`LeaveSimulation`: Viewer+ role
- `PauseSimulation`/`ResumeSimulation`: Operator+ role
- Admin methods (if added): Admin role only

## Error Handling

### Connection Errors
```typescript
{
  error: "connection_failed",
  message: "Authentication required",
  code: "AUTH_001"
}
```

### Method Errors  
```typescript
{
  error: "method_failed", 
  message: "Simulation not found",
  code: "SIM_404"
}
```

## Performance Considerations

### Message Throttling
- Maximum 10 messages per second per simulation
- Batch updates when multiple events occur simultaneously
- Drop intermediate updates if client is behind

### Connection Limits
- Maximum 50 concurrent connections per simulation
- Automatic cleanup of idle connections after 5 minutes
- Load balancing with Redis backplane for production

### Reconnection Strategy
- Automatic reconnection with exponential backoff
- State recovery: send last 5 round updates on reconnect
- Connection heartbeat every 30 seconds