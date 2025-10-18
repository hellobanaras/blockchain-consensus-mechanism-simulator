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
2. Allow configuration of node count (5-100), rounds (1-100), and protocol parameters.
3. Provide real-time log stream of round winners and block hashes.
4. Display final chain height and validations.
5. Offer block explorer with fields: height, hash, prev_hash, proposer, protocol, payload, timestamp.
6. Show analytics: winner distribution, stake/burn trends, vote/quorum stats, wait times.
7. Allow export of analytics data.
8. Provide protocol playground for editing parameters.
9. Support optional payload modes: Supply-Chain (product events), Federated-Learning (model updates).
10. Implement role-based access: Viewer (read-only), Operator (run sims), Admin (manage users).

## Success Criteria

- Users can complete a simulation setup in under 2 minutes.
- Simulations run in real-time with updates every second or less.
- Block explorer loads and displays blocks instantly.
- Analytics charts render within 3 seconds after simulation completion.
- System supports at least 10 concurrent users without performance degradation.
- User satisfaction rating of 4/5 or higher for ease of use.

## Key Entities

- Node: Represents a virtual participant with properties like power, stake, votes.
- Round: A single consensus iteration producing a block.
- Block: Immutable unit with hash, proposer, payload.
- Experiment: A complete simulation run with settings and results.

## Assumptions

- Users are familiar with blockchain basics.
- The application runs in modern web browsers.
- Simulations are synthetic and not for production use.
- Default parameters are sensible for educational purposes.