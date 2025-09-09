using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class Gradient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "facial_hair_color_type",
                table: "profile",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "facial_hair_extended_color",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "hair_color_type",
                table: "profile",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "hair_extended_color",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "facial_hair_color_type",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "facial_hair_extended_color",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "hair_color_type",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "hair_extended_color",
                table: "profile");
        }
    }
}
