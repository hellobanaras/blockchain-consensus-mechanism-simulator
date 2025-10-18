using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Consensus.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SimulationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConsensusAlgorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NodeCount = table.Column<int>(type: "integer", nullable: false),
                    ByzantineNodeCount = table.Column<int>(type: "integer", nullable: false),
                    TargetBlockCount = table.Column<int>(type: "integer", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    Configuration = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    Results = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NetworkTopologies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TopologyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NodeCount = table.Column<int>(type: "integer", nullable: false),
                    AverageConnections = table.Column<double>(type: "double precision", nullable: false),
                    MaxLatencyMs = table.Column<int>(type: "integer", nullable: false),
                    MinLatencyMs = table.Column<int>(type: "integer", nullable: false),
                    PartitionProbability = table.Column<double>(type: "double precision", nullable: false),
                    MessageLossProbability = table.Column<double>(type: "double precision", nullable: false),
                    BandwidthLimitBps = table.Column<long>(type: "bigint", nullable: false),
                    Configuration = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    AdjacencyMatrix = table.Column<int[,]>(type: "integer[]", nullable: true),
                    SimulationRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SimulationRunId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkTopologies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetworkTopologies_SimulationRuns_SimulationRunId",
                        column: x => x.SimulationRunId,
                        principalTable: "SimulationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NetworkTopologies_SimulationRuns_SimulationRunId1",
                        column: x => x.SimulationRunId1,
                        principalTable: "SimulationRuns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Nodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConsensusAlgorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConnectionInfo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    StakeAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ComputationalPower = table.Column<int>(type: "integer", nullable: false),
                    ReputationScore = table.Column<decimal>(type: "numeric", nullable: false),
                    NetworkLatency = table.Column<int>(type: "integer", nullable: false),
                    IsByzantine = table.Column<bool>(type: "boolean", nullable: false),
                    Configuration = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    SimulationRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    LastSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nodes_SimulationRuns_SimulationRunId",
                        column: x => x.SimulationRunId,
                        principalTable: "SimulationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Blocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockNumber = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PreviousHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MerkleRoot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Nonce = table.Column<long>(type: "bigint", nullable: false),
                    Difficulty = table.Column<long>(type: "bigint", nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: false),
                    TransactionCount = table.Column<int>(type: "integer", nullable: false),
                    Data = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ProposerId = table.Column<Guid>(type: "uuid", nullable: true),
                    SimulationRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ProposerId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    SimulationRunId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blocks", x => x.Id);
                    table.CheckConstraint("CK_Block_BlockNumber", "\"BlockNumber\" >= 0");
                    table.ForeignKey(
                        name: "FK_Blocks_Nodes_ProposerId",
                        column: x => x.ProposerId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Blocks_Nodes_ProposerId1",
                        column: x => x.ProposerId1,
                        principalTable: "Nodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Blocks_SimulationRuns_SimulationRunId",
                        column: x => x.SimulationRunId,
                        principalTable: "SimulationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Blocks_SimulationRuns_SimulationRunId1",
                        column: x => x.SimulationRunId1,
                        principalTable: "SimulationRuns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConsensusRounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundNumber = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LeaderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProposedValue = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    AgreedValue = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    ParticipatingNodes = table.Column<int>(type: "integer", nullable: false),
                    VotesReceived = table.Column<int>(type: "integer", nullable: false),
                    PositiveVotes = table.Column<int>(type: "integer", nullable: false),
                    NegativeVotes = table.Column<int>(type: "integer", nullable: false),
                    ConsensusThreshold = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimeoutDuration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Data = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SimulationRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaderId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    SimulationRunId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsensusRounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsensusRounds_Nodes_LeaderId",
                        column: x => x.LeaderId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ConsensusRounds_Nodes_LeaderId1",
                        column: x => x.LeaderId1,
                        principalTable: "Nodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ConsensusRounds_SimulationRuns_SimulationRunId",
                        column: x => x.SimulationRunId,
                        principalTable: "SimulationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsensusRounds_SimulationRuns_SimulationRunId1",
                        column: x => x.SimulationRunId1,
                        principalTable: "SimulationRuns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FromAddress = table.Column<string>(type: "character varying(42)", maxLength: 42, nullable: true),
                    ToAddress = table.Column<string>(type: "character varying(42)", maxLength: 42, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    Fee = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    Nonce = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    GasLimit = table.Column<long>(type: "bigint", nullable: false),
                    GasPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    GasUsed = table.Column<long>(type: "bigint", nullable: false),
                    InputData = table.Column<byte[]>(type: "bytea", nullable: true),
                    Data = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    Signature = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BlockId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionIndex = table.Column<int>(type: "integer", nullable: true),
                    SimulationRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BlockId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    SimulationRunId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.CheckConstraint("CK_Transaction_Amount", "\"Amount\" >= 0");
                    table.CheckConstraint("CK_Transaction_Fee", "\"Fee\" >= 0");
                    table.ForeignKey(
                        name: "FK_Transactions_Blocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "Blocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Transactions_Blocks_BlockId1",
                        column: x => x.BlockId1,
                        principalTable: "Blocks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transactions_SimulationRuns_SimulationRunId",
                        column: x => x.SimulationRunId,
                        principalTable: "SimulationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transactions_SimulationRuns_SimulationRunId1",
                        column: x => x.SimulationRunId1,
                        principalTable: "SimulationRuns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EventLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SimulationRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsensusRoundId = table.Column<Guid>(type: "uuid", nullable: true),
                    BlockId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Info"),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Data = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SimulationRunId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    NodeId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsensusRoundId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    BlockId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventLogs_Blocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "Blocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventLogs_Blocks_BlockId1",
                        column: x => x.BlockId1,
                        principalTable: "Blocks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventLogs_ConsensusRounds_ConsensusRoundId",
                        column: x => x.ConsensusRoundId,
                        principalTable: "ConsensusRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventLogs_ConsensusRounds_ConsensusRoundId1",
                        column: x => x.ConsensusRoundId1,
                        principalTable: "ConsensusRounds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventLogs_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EventLogs_Nodes_NodeId1",
                        column: x => x.NodeId1,
                        principalTable: "Nodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventLogs_SimulationRuns_SimulationRunId",
                        column: x => x.SimulationRunId,
                        principalTable: "SimulationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventLogs_SimulationRuns_SimulationRunId1",
                        column: x => x.SimulationRunId1,
                        principalTable: "SimulationRuns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsensusRoundId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoteType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Value = table.Column<bool>(type: "boolean", nullable: false),
                    ValueHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Data = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    Signature = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Nonce = table.Column<long>(type: "bigint", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric", nullable: false),
                    CastedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NetworkDelayMs = table.Column<int>(type: "integer", nullable: false),
                    NodeId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsensusRoundId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Votes_ConsensusRounds_ConsensusRoundId",
                        column: x => x.ConsensusRoundId,
                        principalTable: "ConsensusRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Votes_ConsensusRounds_ConsensusRoundId1",
                        column: x => x.ConsensusRoundId1,
                        principalTable: "ConsensusRounds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Votes_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Votes_Nodes_NodeId1",
                        column: x => x.NodeId1,
                        principalTable: "Nodes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_CreatedAt",
                table: "Blocks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_Hash",
                table: "Blocks",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_ProposerId",
                table: "Blocks",
                column: "ProposerId");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_ProposerId1",
                table: "Blocks",
                column: "ProposerId1");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_SimulationRunId_BlockNumber",
                table: "Blocks",
                columns: new[] { "SimulationRunId", "BlockNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_SimulationRunId_ProposerId_CreatedAt",
                table: "Blocks",
                columns: new[] { "SimulationRunId", "ProposerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_SimulationRunId1",
                table: "Blocks",
                column: "SimulationRunId1");

            migrationBuilder.CreateIndex(
                name: "IX_ConsensusRounds_LeaderId",
                table: "ConsensusRounds",
                column: "LeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsensusRounds_LeaderId1",
                table: "ConsensusRounds",
                column: "LeaderId1");

            migrationBuilder.CreateIndex(
                name: "IX_ConsensusRounds_SimulationRunId_RoundNumber",
                table: "ConsensusRounds",
                columns: new[] { "SimulationRunId", "RoundNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsensusRounds_SimulationRunId1",
                table: "ConsensusRounds",
                column: "SimulationRunId1");

            migrationBuilder.CreateIndex(
                name: "IX_ConsensusRounds_Status",
                table: "ConsensusRounds",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_BlockId",
                table: "EventLogs",
                column: "BlockId");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_BlockId1",
                table: "EventLogs",
                column: "BlockId1");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_ConsensusRoundId",
                table: "EventLogs",
                column: "ConsensusRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_ConsensusRoundId1",
                table: "EventLogs",
                column: "ConsensusRoundId1");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_CorrelationId",
                table: "EventLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_EventType",
                table: "EventLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_Level",
                table: "EventLogs",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_NodeId",
                table: "EventLogs",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_NodeId1",
                table: "EventLogs",
                column: "NodeId1");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_SimulationRunId_Timestamp",
                table: "EventLogs",
                columns: new[] { "SimulationRunId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_SimulationRunId1",
                table: "EventLogs",
                column: "SimulationRunId1");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkTopologies_SimulationRunId",
                table: "NetworkTopologies",
                column: "SimulationRunId");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkTopologies_SimulationRunId1",
                table: "NetworkTopologies",
                column: "SimulationRunId1");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkTopologies_TopologyType",
                table: "NetworkTopologies",
                column: "TopologyType");

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_Name",
                table: "Nodes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_SimulationRunId_ConsensusAlgorithm_Status",
                table: "Nodes",
                columns: new[] { "SimulationRunId", "ConsensusAlgorithm", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_SimulationRunId_Status",
                table: "Nodes",
                columns: new[] { "SimulationRunId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_Status",
                table: "Nodes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationRuns_ConsensusAlgorithm",
                table: "SimulationRuns",
                column: "ConsensusAlgorithm");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationRuns_CreatedAt",
                table: "SimulationRuns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationRuns_Status",
                table: "SimulationRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BlockId",
                table: "Transactions",
                column: "BlockId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BlockId1",
                table: "Transactions",
                column: "BlockId1");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CreatedAt",
                table: "Transactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Hash",
                table: "Transactions",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_SimulationRunId_BlockId_Status",
                table: "Transactions",
                columns: new[] { "SimulationRunId", "BlockId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_SimulationRunId_Status",
                table: "Transactions",
                columns: new[] { "SimulationRunId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_SimulationRunId1",
                table: "Transactions",
                column: "SimulationRunId1");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_CastedAt",
                table: "Votes",
                column: "CastedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_ConsensusRoundId_NodeId",
                table: "Votes",
                columns: new[] { "ConsensusRoundId", "NodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Votes_ConsensusRoundId_VoteType_CastedAt",
                table: "Votes",
                columns: new[] { "ConsensusRoundId", "VoteType", "CastedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Votes_ConsensusRoundId1",
                table: "Votes",
                column: "ConsensusRoundId1");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_NodeId",
                table: "Votes",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_NodeId1",
                table: "Votes",
                column: "NodeId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventLogs");

            migrationBuilder.DropTable(
                name: "NetworkTopologies");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Votes");

            migrationBuilder.DropTable(
                name: "Blocks");

            migrationBuilder.DropTable(
                name: "ConsensusRounds");

            migrationBuilder.DropTable(
                name: "Nodes");

            migrationBuilder.DropTable(
                name: "SimulationRuns");
        }
    }
}
