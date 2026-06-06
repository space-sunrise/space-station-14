using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RoadmapLikes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ui_likes",
                columns: table => new
                {
                    scope_id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    item_id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    player_user_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ui_likes", x => new { x.scope_id, x.item_id, x.player_user_id });
                });

            migrationBuilder.CreateIndex(
                name: "IX_ui_likes_player_user_id_scope_id",
                table: "ui_likes",
                columns: new[] { "player_user_id", "scope_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ui_likes");
        }
    }
}
