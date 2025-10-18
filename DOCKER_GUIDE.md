# Docker Deployment Guide

## Overview
The Blockchain Consensus Mechanism Simulator is now fully containerized using Docker and Docker Compose. The application runs in containers with a PostgreSQL database and is accessible via your web browser.

## Architecture
- **Web Application**: Blazor Server running on .NET 9.0
- **Database**: PostgreSQL 16 with persistent data
- **Database Admin**: pgAdmin 4 for database management
- **Network**: Isolated Docker network for security

## Getting Started

### Prerequisites
- Docker installed and running
- Docker Compose available

### Quick Start
1. **Start the application:**
   ```bash
   docker-compose up --build -d
   ```

2. **Apply database migrations:**
   ```bash
   ASPNETCORE_ENVIRONMENT=Development ConnectionStrings__DefaultConnection="Host=localhost;Database=consensusdb;Username=consensus_user;Password=consensus_password;Port=5432" dotnet ef database update --project src/Consensus.Data --startup-project src/Consensus.Web
   ```

3. **Access the application:**
   - **Main Application**: http://localhost:3000
   - **Database Admin**: http://localhost:5050 (login: admin@consensus.com / admin_password)

## Port Mapping
| Service | Container Port | Host Port | Purpose |
|---------|---------------|-----------|---------|
| Web App | 8080 | 3000 | Main application UI |
| PostgreSQL | 5432 | 5432 | Database access |
| pgAdmin | 80 | 5050 | Database management |

## Container Management

### Start Services
```bash
# Start all services in detached mode
docker-compose up -d

# Start with rebuild
docker-compose up --build -d

# Start with logs visible
docker-compose up
```

### Stop Services
```bash
# Stop all services
docker-compose down

# Stop and remove volumes (⚠️ This will delete all data)
docker-compose down -v
```

### View Logs
```bash
# View all logs
docker-compose logs

# View specific service logs
docker logs consensus-web
docker logs consensus-postgres
docker logs consensus-pgadmin

# Follow logs in real-time
docker logs -f consensus-web
```

### Container Status
```bash
# View running containers
docker ps

# View all containers (including stopped)
docker ps -a
```

## Development Workflow

### Making Code Changes
1. Make your changes to the source code
2. Rebuild and restart the web container:
   ```bash
   docker-compose build web
   docker-compose up -d web
   ```

### Database Operations
```bash
# Apply migrations
ASPNETCORE_ENVIRONMENT=Development ConnectionStrings__DefaultConnection="Host=localhost;Database=consensusdb;Username=consensus_user;Password=consensus_password;Port=5432" dotnet ef database update --project src/Consensus.Data --startup-project src/Consensus.Web

# Create new migration
dotnet ef migrations add MigrationName --project src/Consensus.Data --startup-project src/Consensus.Web

# Connect to database directly
docker exec -it consensus-postgres psql -U consensus_user -d consensusdb
```

## Database Access

### Connection Details
- **Host**: localhost (from host machine) or postgres (from containers)
- **Port**: 5432
- **Database**: consensusdb
- **Username**: consensus_user
- **Password**: consensus_password

### Using pgAdmin
1. Open http://localhost:5050 in your browser
2. Login with: admin@consensus.com / admin_password
3. Add a new server connection:
   - **Name**: Consensus DB
   - **Host**: postgres
   - **Port**: 5432
   - **Database**: consensusdb
   - **Username**: consensus_user
   - **Password**: consensus_password

## Troubleshooting

### Common Issues

**Port Already in Use**
```bash
# Check what's using the port
lsof -i :3000

# Kill the process using the port
kill -9 <PID>
```

**Database Connection Issues**
```bash
# Check if PostgreSQL is running
docker logs consensus-postgres

# Verify database is accessible
docker exec -it consensus-postgres pg_isready -U consensus_user -d consensusdb
```

**Application Not Starting**
```bash
# Check application logs
docker logs consensus-web

# Rebuild the application
docker-compose build web --no-cache
docker-compose up -d web
```

**Out of Disk Space**
```bash
# Clean up unused Docker resources
docker system prune -a

# Remove unused volumes
docker volume prune
```

### Health Checks
The containers include health checks that you can monitor:

```bash
# Check container health status
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# View detailed health check logs
docker inspect consensus-postgres | grep -A 10 Health
```

## Production Considerations

### Security
- Change default database credentials in production
- Use secrets management for sensitive data
- Enable HTTPS/TLS for the web application
- Restrict network access using Docker networks

### Performance
- Allocate sufficient memory to containers
- Use persistent volumes for database data
- Monitor container resource usage
- Consider using Redis for SignalR scaling in multi-instance deployments

### Monitoring
- Set up container monitoring (Prometheus, Grafana)
- Configure log aggregation (ELK stack)
- Implement health check endpoints
- Set up alerts for container failures

## File Structure
```
├── Dockerfile                 # Web application container definition
├── docker-compose.yml        # Multi-service orchestration
├── scripts/
│   └── init-db.sql           # Database initialization script
├── .dockerignore             # Files to exclude from Docker build
└── src/
    └── Consensus.Web/
        ├── appsettings.json  # Application configuration
        └── Program.cs        # Application startup with DB config
```

## Next Steps
1. Access the application at http://localhost:3000
2. Start implementing consensus protocols
3. Use pgAdmin at http://localhost:5050 to explore the database schema
4. Monitor application logs for any issues
5. Begin testing consensus simulations

The application is now fully containerized and ready for development and testing of blockchain consensus mechanisms!