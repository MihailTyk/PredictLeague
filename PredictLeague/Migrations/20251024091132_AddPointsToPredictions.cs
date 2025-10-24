using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PredictLeague.Migrations
{
    /// <inheritdoc />
    public partial class AddPointsToPredictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "Prediction",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Points",
                table: "Prediction");
        }
    }
}
