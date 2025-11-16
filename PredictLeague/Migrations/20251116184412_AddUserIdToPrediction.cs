using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PredictLeague.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToPrediction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserName",
                table: "Prediction");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Prediction",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Prediction_UserId",
                table: "Prediction",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Prediction_AspNetUsers_UserId",
                table: "Prediction",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prediction_AspNetUsers_UserId",
                table: "Prediction");

            migrationBuilder.DropIndex(
                name: "IX_Prediction_UserId",
                table: "Prediction");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Prediction");

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "Prediction",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
