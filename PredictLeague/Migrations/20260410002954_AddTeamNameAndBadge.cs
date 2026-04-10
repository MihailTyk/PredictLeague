using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PredictLeague.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamNameAndBadge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TeamBadgeUrl",
                table: "UserTeamSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TeamName",
                table: "UserTeamSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeamBadgeUrl",
                table: "UserTeamSettings");

            migrationBuilder.DropColumn(
                name: "TeamName",
                table: "UserTeamSettings");
        }
    }
}
