using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AHelpDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ahelp_messages",
                columns: table => new
                {
                    ahelp_messages_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    receiver_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    sender_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    message = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    play_sound = table.Column<bool>(type: "INTEGER", nullable: false),
                    admin_only = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ahelp_messages", x => x.ahelp_messages_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ahelp_messages_receiver_user_id",
                table: "ahelp_messages",
                column: "receiver_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ahelp_messages");
        }
    }
}
