using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddCharacterRecordFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "birth_day",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "birth_month",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "employment_record",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "medical_record",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "patronymic",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "security_record",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "birth_day",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "birth_month",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "employment_record",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "medical_record",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "patronymic",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "security_record",
                table: "profile");
        }
    }
}
