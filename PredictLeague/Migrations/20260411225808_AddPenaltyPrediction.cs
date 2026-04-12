using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PredictLeague.Migrations
{
    /// <inheritdoc />
    public partial class AddPenaltyPrediction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PredictedPenalty",
                table: "Prediction",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HadPenalty",
                table: "Match",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PredictedPenalty",
                table: "Prediction");

            migrationBuilder.DropColumn(
                name: "HadPenalty",
                table: "Match");
        }
    }
}
