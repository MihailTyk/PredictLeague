using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PredictLeague.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalsAndAssistsToUserPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Assists",
                table: "UserPlayers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Goals",
                table: "UserPlayers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ActualCorners",
                table: "Match",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActualGoalscorers",
                table: "Match",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActualOffsides",
                table: "Match",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActualRedCards",
                table: "Match",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActualYellowCards",
                table: "Match",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Assists",
                table: "UserPlayers");

            migrationBuilder.DropColumn(
                name: "Goals",
                table: "UserPlayers");

            migrationBuilder.DropColumn(
                name: "ActualCorners",
                table: "Match");

            migrationBuilder.DropColumn(
                name: "ActualGoalscorers",
                table: "Match");

            migrationBuilder.DropColumn(
                name: "ActualOffsides",
                table: "Match");

            migrationBuilder.DropColumn(
                name: "ActualRedCards",
                table: "Match");

            migrationBuilder.DropColumn(
                name: "ActualYellowCards",
                table: "Match");
        }
    }
}
