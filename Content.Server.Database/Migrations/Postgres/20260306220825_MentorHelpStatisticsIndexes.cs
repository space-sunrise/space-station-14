using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class MentorHelpStatisticsIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_mentor_help_tickets_closed_at_assigned_to_user_id",
                table: "mentor_help_tickets",
                columns: new[] { "closed_at", "assigned_to_user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_mentor_help_messages_sent_at_sender_user_id",
                table: "mentor_help_messages",
                columns: new[] { "sent_at", "sender_user_id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_mentor_help_tickets_closed_at_assigned_to_user_id",
                table: "mentor_help_tickets");

            migrationBuilder.DropIndex(
                name: "IX_mentor_help_messages_sent_at_sender_user_id",
                table: "mentor_help_messages");
        }
    }
}
