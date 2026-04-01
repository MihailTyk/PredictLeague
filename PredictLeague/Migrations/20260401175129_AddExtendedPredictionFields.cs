using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PredictLeague.Migrations
{
    /// <inheritdoc />
    public partial class AddExtendedPredictionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnytimeGoalscorer",
                table: "Prediction",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoalScoringPrediction",
                table: "Prediction",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PredictedCorners",
                table: "Prediction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PredictedOffsides",
                table: "Prediction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PredictedRedCards",
                table: "Prediction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PredictedYellowCards",
                table: "Prediction",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnytimeGoalscorer",
                table: "Prediction");

            migrationBuilder.DropColumn(
                name: "GoalScoringPrediction",
                table: "Prediction");

            migrationBuilder.DropColumn(
                name: "PredictedCorners",
                table: "Prediction");

            migrationBuilder.DropColumn(
                name: "PredictedOffsides",
                table: "Prediction");

            migrationBuilder.DropColumn(
                name: "PredictedRedCards",
                table: "Prediction");

            migrationBuilder.DropColumn(
                name: "PredictedYellowCards",
                table: "Prediction");
        }
    }
}
