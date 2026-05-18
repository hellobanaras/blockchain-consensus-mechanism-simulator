using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Consensus.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSimulationSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RandomSeed",
                table: "SimulationRuns",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RandomSeed",
                table: "SimulationRuns");
        }
    }
}
