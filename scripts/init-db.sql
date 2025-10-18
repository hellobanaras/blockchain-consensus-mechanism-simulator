-- Database initialization script for Consensus Mechanism Simulator
-- This script runs when the PostgreSQL container starts for the first time

-- Enable necessary extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Create additional schemas if needed
-- CREATE SCHEMA IF NOT EXISTS consensus_audit;

-- Set up database configuration
ALTER DATABASE consensusdb SET timezone TO 'UTC';

-- Create indexes that might be needed before Entity Framework migrations
-- (These will be created by EF migrations, but having them here as reference)

-- Grant permissions (already handled by POSTGRES_USER environment variable)
-- GRANT ALL PRIVILEGES ON DATABASE consensusdb TO consensus_user;

-- Log initialization completion
DO $$
BEGIN
    RAISE NOTICE 'Database initialization completed successfully';
END $$;