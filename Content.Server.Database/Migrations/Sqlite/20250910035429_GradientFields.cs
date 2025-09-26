using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class GradientFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "all_markings_gradient_direction",
                table: "profile",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "all_markings_gradient_enabled",
                table: "profile",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "all_markings_gradient_secondary_color",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "#FFFFFF");

            migrationBuilder.AddColumn<int>(
                name: "facial_hair_gradient_direction",
                table: "profile",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "facial_hair_gradient_enabled",
                table: "profile",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "facial_hair_gradient_secondary_color",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "#FFFFFF");

            migrationBuilder.AddColumn<int>(
                name: "hair_gradient_direction",
                table: "profile",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "hair_gradient_enabled",
                table: "profile",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "hair_gradient_secondary_color",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "#FFFFFF");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "all_markings_gradient_direction",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "all_markings_gradient_enabled",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "all_markings_gradient_secondary_color",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "facial_hair_gradient_direction",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "facial_hair_gradient_enabled",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "facial_hair_gradient_secondary_color",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "hair_gradient_direction",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "hair_gradient_enabled",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "hair_gradient_secondary_color",
                table: "profile");
        }
    }
}
