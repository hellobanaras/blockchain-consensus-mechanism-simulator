using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Consensus.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "SimulationRuns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRounds",
                table: "SimulationRuns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Progress",
                table: "SimulationRuns",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "SimulationRuns");

            migrationBuilder.DropColumn(
                name: "MaxRounds",
                table: "SimulationRuns");

            migrationBuilder.DropColumn(
                name: "Progress",
                table: "SimulationRuns");
        }
    }
}
