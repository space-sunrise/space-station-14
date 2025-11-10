using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class GradientUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "hair_extended_color",
                table: "profile",
                newName: "hair_gradient_secondary_color");

            migrationBuilder.RenameColumn(
                name: "hair_color_type",
                table: "profile",
                newName: "hair_gradient_direction");

            migrationBuilder.RenameColumn(
                name: "facial_hair_extended_color",
                table: "profile",
                newName: "facial_hair_gradient_secondary_color");

            migrationBuilder.RenameColumn(
                name: "facial_hair_color_type",
                table: "profile",
                newName: "facial_hair_gradient_direction");

            migrationBuilder.AddColumn<int>(
                name: "all_markings_gradient_direction",
                table: "profile",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "all_markings_gradient_enabled",
                table: "profile",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "all_markings_gradient_secondary_color",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "facial_hair_gradient_enabled",
                table: "profile",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "hair_gradient_enabled",
                table: "profile",
                type: "boolean",
                nullable: false,
                defaultValue: false);
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
                name: "facial_hair_gradient_enabled",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "hair_gradient_enabled",
                table: "profile");

            migrationBuilder.RenameColumn(
                name: "hair_gradient_secondary_color",
                table: "profile",
                newName: "hair_extended_color");

            migrationBuilder.RenameColumn(
                name: "hair_gradient_direction",
                table: "profile",
                newName: "hair_color_type");

            migrationBuilder.RenameColumn(
                name: "facial_hair_gradient_secondary_color",
                table: "profile",
                newName: "facial_hair_extended_color");

            migrationBuilder.RenameColumn(
                name: "facial_hair_gradient_direction",
                table: "profile",
                newName: "facial_hair_color_type");
        }
    }
}
