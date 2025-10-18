# Blockchain Consensus Mechanism Simulator

A comprehensive educational platform for simulating and analyzing various blockchain consensus protocols including Proof of Work (PoW), Proof of Stake (PoS), Practical Byzantine Fault Tolerance (PBFT), Raft, and more. Built with Blazor Server, ASP.NET Core, and Entity Framework Core.

## 🎯 Project Overview

The Consensus Lab provides an interactive environment for:
- **Protocol Simulation**: Run consensus algorithms with configurable parameters
- **Real-time Visualization**: Watch consensus rounds and block creation in real-time
- **Blockchain Explorer**: Navigate and inspect the generated blockchain
- **Analytics Dashboard**: Analyze performance metrics and consensus behavior
- **Educational Tool**: Learn consensus mechanisms through hands-on experimentation

## 🚀 Current Implementation Status

### ✅ **Completed Features**

#### **Infrastructure & Architecture**
- **Multi-project .NET 9.0 solution** with clean architecture separation
- **Entity Framework Core** with PostgreSQL database support
- **Docker containerization** with Docker Compose for easy deployment
- **CI/CD workflows** with GitHub Actions for automated testing and deployment
- **Database migrations** with initial schema for core entities

#### **Frontend - Blazor Server UI** 
- **🏠 Dashboard**: Comprehensive simulation overview and control center
  - Real-time statistics cards (Active Nodes, Running Simulations, etc.)
  - Quick action buttons for starting simulations
  - Recent activity feed with event logging
  - Live simulations monitoring panel

- **🔧 Node Management**: Complete node administration interface
  - Node statistics and health monitoring  
  - Filterable node table with search functionality
  - Add/Edit node modals with full CRUD operations
  - Node status management and geographic distribution

- **🧩 Block Explorer**: Blockchain viewing interface
  - Blockchain statistics and metrics
  - Advanced search and filtering capabilities
  - Detailed block information modals
  - Transaction integration and links
  - Block validation status tracking

- **▶️ Simulations**: Simulation management hub
  - Simulation creation wizard with algorithm selection
  - Real-time simulation status tracking
  - Progress monitoring and control (pause/resume/stop)
  - Results analysis and simulation cloning
  - Algorithm comparison and performance metrics

- **📊 Analytics**: Comprehensive analytics dashboard
  - Performance metrics and KPI tracking
  - Algorithm comparison matrix
  - Network statistics and fault analysis
  - AI-powered insights and recommendations
  - Real-time trend analysis and alerting

- **💚 Network Health Monitor**: System health monitoring
  - Overall network health scoring
  - Live performance monitoring
  - Geographic node distribution  
  - System alerts and diagnostics
  - Performance trend analysis

#### **Backend - Data Layer**
- **Core Entities**: Node, Block, Transaction, SimulationRun, EventLog
- **Enum Definitions**: ConsensusAlgorithm, NodeStatus, SimulationStatus, etc.
- **Database Context**: Configured with PostgreSQL provider
- **Repository Pattern**: Base structure for data access
- **Migration Support**: Initial database schema creation

#### **API Layer**
- **ASP.NET Core API** structure with Swagger/OpenAPI documentation
- **RESTful endpoints** foundation for simulation management
- **SignalR Hub contracts** defined for real-time updates

### 🔄 **In Progress**

#### **Core Simulation Engine** 
- Round and Experiment entity implementations
- SimulationService for managing consensus rounds
- Protocol implementations (starting with PoET)
- Real-time update infrastructure via SignalR

#### **Integration Layer**
- Service layer connecting UI to data access
- Background services for simulation processing
- API endpoints for external integration

### 📋 **Planned Features**

#### **Consensus Protocol Support**
- **Proof of Work (PoW)**: Mining simulation with adjustable difficulty
- **Proof of Stake (PoS)**: Stake-based selection with slashing
- **Delegated Proof of Stake (DPoS)**: Delegate voting and validation
- **Proof of Authority (PoA)**: Authority-based consensus
- **Practical Byzantine Fault Tolerance (PBFT)**: Byzantine fault tolerance
- **Proof of Elapsed Time (PoET)**: Intel SGX-based consensus simulation
- **Proof of Burn (PoB)**: Token burning mechanism

#### **Advanced Features**
- **Protocol Playground**: Parameter configuration and testing
- **Payload Modes**: Supply-Chain and Federated Learning scenarios
- **User Management**: Role-based access (Viewer, Operator, Admin)
- **Data Export**: CSV/JSON analytics export functionality
- **Performance Optimization**: Support for large-scale simulations

## 🛠 **Technology Stack**

### **Frontend**
- **Blazor Server**: Interactive server-side rendering
- **Bootstrap 5**: Responsive UI framework with custom theming
- **SignalR**: Real-time communication for live updates
- **Chart.js**: Data visualization and analytics charts

### **Backend** 
- **ASP.NET Core 9.0**: Web API and application framework
- **Entity Framework Core**: Object-relational mapping and data access
- **PostgreSQL**: Primary database for production
- **SQLite**: Development database option

### **Infrastructure**
- **Docker**: Containerization for deployment
- **Docker Compose**: Multi-service orchestration
- **GitHub Actions**: CI/CD pipeline automation
- **Serilog**: Structured logging framework

### **Testing**
- **NUnit/xUnit**: Unit testing frameworks
- **WebApplicationFactory**: Integration testing
- **Playwright**: End-to-end UI testing

## 🚦 **Getting Started**

### **Prerequisites**
- .NET 9.0 SDK
- PostgreSQL (or Docker for containerized setup)
- Node.js (for client-side assets)
- Git

### **Quick Start with Docker**

1. **Clone the repository**
   ```bash
   git clone https://github.com/hellobanaras/blockchain-consensus-mechanism-simulator.git
   cd blockchain-consensus-mechanism-simulator
   ```

2. **Start with Docker Compose**
   ```bash
   docker-compose up -d
   ```

3. **Access the application**
   - Web UI: http://localhost:3000
   - API Documentation: http://localhost:3000/swagger

### **Development Setup**

1. **Restore dependencies**
   ```bash
   dotnet restore
   ```

2. **Update database**
   ```bash
   cd src/Consensus.Web
   dotnet ef database update
   ```

3. **Run the application**
   ```bash
   dotnet run --project src/Consensus.Web
   ```

## 📖 **Documentation**

- **[Feature Specification](./specs/001-consensus-lab-implementation/spec.md)**: Detailed requirements and user stories
- **[Implementation Plan](./specs/001-consensus-lab-implementation/plan.md)**: Architecture and development roadmap  
- **[Task Breakdown](./specs/001-consensus-lab-implementation/tasks.md)**: Detailed implementation tasks
- **[Data Model](./specs/001-consensus-lab-implementation/data-model.md)**: Entity relationships and schema
- **[API Contracts](./specs/001-consensus-lab-implementation/contracts/)**: API specifications and SignalR hubs
- **[Quick Start Guide](./specs/001-consensus-lab-implementation/quickstart.md)**: Getting up and running quickly
- **[Docker Guide](./DOCKER_GUIDE.md)**: Container deployment instructions

## 🏗 **Project Structure**

```
├── src/
│   ├── Consensus.Web/           # Blazor Server UI application
│   │   ├── Components/Pages/    # Razor pages (Dashboard, Explorer, etc.)
│   │   ├── Components/Layout/   # Layout components and navigation
│   │   └── wwwroot/            # Static assets and Bootstrap theme
│   ├── Consensus.Core/         # Business logic and domain entities  
│   │   ├── Entities/           # Data models (Node, Block, Round, etc.)
│   │   ├── Enums/             # Consensus algorithms and status types
│   │   └── Interfaces/        # Service contracts and abstractions
│   ├── Consensus.Api/          # REST API layer
│   └── Consensus.Data/         # Data access with Entity Framework
│       ├── Migrations/         # Database schema migrations
│       └── Context/           # DbContext configuration
├── tests/                      # Test projects for all layers
├── specs/                      # Feature specifications and documentation
├── scripts/                    # Database and deployment scripts
└── docker-compose.yml          # Container orchestration
```

## 🎯 **Key Features Implemented**

### **Dashboard & Monitoring**
- Real-time simulation statistics and KPIs
- Live activity feeds with event logging
- Quick simulation launch capabilities
- Network health monitoring with visual indicators

### **Node Management** 
- Comprehensive node lifecycle management
- Geographic distribution visualization
- Node status tracking and health monitoring
- Bulk operations and filtering capabilities

### **Blockchain Explorer**
- Interactive blockchain navigation
- Detailed block and transaction inspection
- Advanced search and filtering options
- Block validation status and metadata

### **Simulation Management**
- Intuitive simulation creation wizard
- Real-time progress tracking and control
- Algorithm comparison and benchmarking
- Simulation cloning and result analysis

### **Analytics & Insights**
- Performance metrics across protocols
- Algorithm comparison matrices
- Network fault tolerance analysis
- AI-powered recommendations and insights

## 🔧 **Development Status**

### **Phase 1 ✅ Complete**: Project Setup & Infrastructure
- Solution structure and project configuration
- Docker containerization and CI/CD pipelines
- Database schema and Entity Framework setup
- UI framework and component architecture

### **Phase 2 🔄 In Progress**: Core Simulation Engine
- Consensus protocol implementations
- Simulation orchestration services  
- Real-time update infrastructure
- Service layer integration

### **Phase 3 📋 Planned**: Advanced Features & Polish
- Additional consensus protocols
- User authentication and authorization
- Performance optimization and load testing
- Comprehensive documentation and help system

## 🤝 **Contributing**

We welcome contributions! Please see our [Contributing Guidelines](./CONTRIBUTING.md) for details on:
- Code standards and style guidelines
- Testing requirements and procedures
- Pull request process and review criteria
- Issue reporting and feature requests

## 📄 **License**

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.

## 🙋 **Support & Community**

- **Issues**: Report bugs and request features via [GitHub Issues](https://github.com/hellobanaras/blockchain-consensus-mechanism-simulator/issues)
- **Discussions**: Join community discussions and ask questions
- **Documentation**: Comprehensive guides in the `/specs` directory
- **Examples**: Sample configurations and use cases

---

**Built for Education** 📚 | **Powered by .NET** 🔷 | **Open Source** 💖
