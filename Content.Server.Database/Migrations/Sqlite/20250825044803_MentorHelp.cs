using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class MentorHelp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mentor_help_tickets",
                columns: table => new
                {
                    mentor_help_tickets_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    player_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    assigned_to_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    subject = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    closed_by_user_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    round_id = table.Column<int>(type: "INTEGER", nullable: true),
                    server_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mentor_help_tickets", x => x.mentor_help_tickets_id);
                });

            migrationBuilder.CreateTable(
                name: "mentor_help_messages",
                columns: table => new
                {
                    mentor_help_messages_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ticket_id = table.Column<int>(type: "INTEGER", nullable: false),
                    sender_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    message = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    is_staff_only = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mentor_help_messages", x => x.mentor_help_messages_id);
                    table.ForeignKey(
                        name: "FK_mentor_help_messages_mentor_help_tickets_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "mentor_help_tickets",
                        principalColumn: "mentor_help_tickets_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mentor_help_messages_sent_at",
                table: "mentor_help_messages",
                column: "sent_at");

            migrationBuilder.CreateIndex(
                name: "IX_mentor_help_messages_ticket_id",
                table: "mentor_help_messages",
                column: "ticket_id");

            migrationBuilder.CreateIndex(
                name: "IX_mentor_help_tickets_assigned_to_user_id",
                table: "mentor_help_tickets",
                column: "assigned_to_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_mentor_help_tickets_player_id",
                table: "mentor_help_tickets",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_mentor_help_tickets_status",
                table: "mentor_help_tickets",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mentor_help_messages");

            migrationBuilder.DropTable(
                name: "mentor_help_tickets");
        }
    }
}
