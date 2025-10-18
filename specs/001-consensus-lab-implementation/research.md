# Research Phase: Consensus Lab

## Technology Stack Decisions

### Decision: Blazor Server over Blazor WebAssembly
**Rationale**: 
- Real-time SignalR updates work seamlessly with Blazor Server
- Server-side execution provides better performance for complex consensus calculations
- Reduces client-side payload and startup time
- Better suited for dashboard/monitoring applications with frequent server interaction

**Alternatives considered**: 
- Blazor WebAssembly: Rejected due to SignalR complexity and performance overhead for computation-heavy simulations
- React/Angular SPA: Rejected to maintain C# throughout stack and leverage .NET ecosystem

### Decision: Entity Framework Core with PostgreSQL
**Rationale**:
- EF Core provides robust migration system for evolving data models
- PostgreSQL offers excellent JSON support for flexible protocol parameters
- Strong performance for analytical queries (winner distributions, metrics)
- Docker and Azure deployment support

**Alternatives considered**:
- MongoDB: Rejected due to relational nature of blocks, rounds, and experiments
- SQL Server: Rejected for cost considerations in cloud deployment

### Decision: SignalR for Real-time Updates
**Rationale**:
- Native integration with Blazor Server
- Automatic fallback mechanisms (WebSockets -> Server-Sent Events -> Long Polling)
- Built-in connection management and reconnection logic
- Scales well with Azure SignalR Service

**Alternatives considered**:
- Server-Sent Events only: Rejected due to limited browser support and no bidirectional communication
- WebSockets directly: Rejected due to implementation complexity

### Decision: Minimal API over MVC Controllers
**Rationale**:
- Simpler setup for straightforward CRUD operations
- Better performance and reduced memory footprint
- Built-in OpenAPI generation
- Easier testing with WebApplicationFactory

**Alternatives considered**:
- Full MVC Controllers: Overkill for simple REST API needs
- GraphQL: Rejected due to added complexity for this use case

## Consensus Protocol Research

### PoET (Proof of Elapsed Time) Implementation
**Approach**: HMAC-based wait time verification using cryptographic proofs
**Key Libraries**: System.Security.Cryptography for HMAC generation
**Reference**: Intel SGX attestation patterns adapted for simulation

### PBFT (Practical Byzantine Fault Tolerance) Implementation  
**Approach**: Three-phase protocol (pre-prepare, prepare, commit) with 2/3+1 quorum requirement
**Fault Injection**: Configurable faulty nodes for educational visualization
**Reference**: Castro & Liskov original paper implementation patterns

### Proof of Burn Economic Model
**Approach**: Token burning with economic incentives and weighted selection
**Balance Tracking**: Persistent stake/burn state across rounds
**Reference**: Counterparty and Slimcoin economic models

## Performance Optimization Research

### Real-time Update Strategy
**Approach**: Batch SignalR updates per round rather than per-node to reduce message frequency
**Connection Scaling**: Connection groups per simulation to isolate updates
**Fallback**: Polling mechanism for analytics when SignalR unavailable

### Chart Rendering Performance
**Decision**: Chart.js via JavaScript interop over server-side rendering
**Rationale**: Better performance for frequent updates, client-side animation support
**Memory Management**: Dispose chart instances on component cleanup

## Security Considerations

### Rate Limiting Strategy
**Implementation**: ASP.NET Core rate limiting middleware
**Limits**: 5 simulation starts per minute per user, 100 API calls per minute
**Monitoring**: Structured logging with Serilog for abuse detection

### Input Validation
**Approach**: FluentValidation for complex protocol parameter validation
**Range Checking**: Node counts (5-100), rounds (1-100), protocol-specific bounds
**Sanitization**: JSON parameter validation to prevent injection attacks