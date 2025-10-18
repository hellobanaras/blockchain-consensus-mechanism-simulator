# Feature Specification: Consensus Lab

## User Scenarios & Testing

### Primary User Flow: Run Consensus Simulation

1. User selects a consensus protocol (e.g., PoET).
2. User configures simulation parameters (node count, rounds, protocol-specific settings).
3. User starts the simulation.
4. User watches real-time updates of rounds, winners, and block creation.
5. User views the growing blockchain and analytics.

### Secondary Flows

- Explore block details in the block explorer.
- Configure protocol parameters in the playground.
- Toggle payload modes for additional context.

Testing: Acceptance tests for each user flow, ensuring real-time updates and correct block chain.

## Functional Requirements

1. Support selection of consensus protocols: PoW, PoS, DPoS, PoA, PBFT, PoET, PoB.
2. Allow configuration of node count (5-100), rounds (1-100), and protocol-specific parameters:
   - **PoW**: Difficulty target, hash algorithm selection
   - **PoS**: Minimum stake amount, stake slashing rules
   - **DPoS**: Delegate count, voting periods
   - **PoA**: Authority node designation, rotation schedule
   - **PBFT**: Byzantine fault tolerance threshold, timeout values
   - **PoET**: Wait time bounds (min/max), attestation simulation parameters
   - **PoB**: Burn rate, token supply settings
3. Provide real-time log stream of round winners and block hashes.
4. Display final chain height and validations.
5. Offer block explorer with fields: height, hash, prev_hash, proposer, protocol, payload, timestamp.
6. Show analytics: winner distribution, stake/burn trends, vote/quorum stats, wait times.
7. Allow export of analytics data in CSV and JSON formats.
8. Provide protocol playground for editing and persisting parameter configurations.
9. Support optional payload modes: Supply-Chain (product events), Federated-Learning (model updates).
10. Implement role-based access: Viewer (read-only), Operator (run sims), Admin (manage users).

## Success Criteria

- Users can complete a simulation setup in under 2 minutes.
- Simulations provide real-time updates with maximum 500ms latency (SignalR broadcast to all connected clients).
- Block explorer loads initial view in under 1 second and displays individual blocks within 200ms.
- Analytics charts render within 3 seconds for simulations up to 50 nodes and 50 rounds; 5 seconds for larger simulations (100 nodes/rounds).
- System supports at least 10 concurrent users running simultaneous simulations without >20% performance degradation.
- User satisfaction rating of 4/5 or higher for ease of use in usability testing.

## Key Entities

*Detailed entity specifications are defined in [data-model.md](./data-model.md)*

- **Node**: Virtual participant with protocol-specific properties (power, stake, votes, role)
- **Round**: Single consensus iteration with timing, parameters, and winner tracking  
- **Block**: Immutable blockchain unit with hash chain linkage and protocol metadata
- **Experiment**: Complete simulation run configuration with results and status tracking

## Assumptions

- Users are familiar with blockchain basics.
- The application runs in modern web browsers.
- Simulations are synthetic and not for production use.
- Default parameters are sensible for educational purposes.