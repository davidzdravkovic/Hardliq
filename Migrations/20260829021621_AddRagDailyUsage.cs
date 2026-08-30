using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManager.Migrations
{
    /// <inheritdoc />
    public partial class AddRagDailyUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RagDailyUsages",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    UsageDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RagDailyUsages", x => new { x.UserId, x.UsageDate });
                    table.ForeignKey(
                        name: "FK_RagDailyUsages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RagDailyUsages");
        }
    }
}
