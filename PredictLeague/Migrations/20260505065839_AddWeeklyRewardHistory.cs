using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PredictLeague.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyRewardHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeeklyRewardHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    PointsAwarded = table.Column<int>(type: "int", nullable: false),
                    DistributedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyRewardHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyRewardHistory_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyRewardHistory_UserId",
                table: "WeeklyRewardHistory",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeeklyRewardHistory");
        }
    }
}
