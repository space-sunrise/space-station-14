using System.Reflection;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    [DbContext(typeof(PostgresServerDbContext))]
    [Migration("20260306220825_MentorHelpStatisticsIndexes")]
    partial class MentorHelpStatisticsIndexes
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            InvokePreviousBuildTargetModel<OldGradients>(modelBuilder);

            modelBuilder.Entity("Content.Server.Database.MentorHelpMessage", b =>
                {
                    b.HasIndex("SentAt", "SenderUserId")
                        .HasDatabaseName("IX_mentor_help_messages_sent_at_sender_user_id");
                });

            modelBuilder.Entity("Content.Server.Database.MentorHelpTicket", b =>
                {
                    b.HasIndex("ClosedAt", "AssignedToUserId").HasDatabaseName("IX_mentor_help_tickets_closed_at_assigned_to_user_id");
                });

        }

        private static void InvokePreviousBuildTargetModel<TMigration>(ModelBuilder modelBuilder) where TMigration : Migration, new()
        {
            var method = typeof(TMigration).GetMethod("BuildTargetModel", BindingFlags.Instance | BindingFlags.NonPublic);
            method!.Invoke(new TMigration(), new object[] { modelBuilder });
        }
    }
}
