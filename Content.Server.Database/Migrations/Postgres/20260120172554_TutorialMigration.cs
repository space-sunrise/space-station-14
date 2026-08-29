using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class TutorialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tutorial_completion",
                columns: table => new
                {
                    player_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tutorial_id = table.Column<string>(type: "text", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completion_count = table.Column<int>(type: "integer", nullable: false),
                    account_age_days = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tutorial_completion", x => new { x.player_user_id, x.tutorial_id });
                });

            migrationBuilder.CreateIndex(
                name: "IX_tutorial_completion_player_user_id",
                table: "tutorial_completion",
                column: "player_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tutorial_completion_tutorial_id",
                table: "tutorial_completion",
                column: "tutorial_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tutorial_completion");
        }
    }
}
