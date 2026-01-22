using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DbMigrator.Migrations
{
    /// <inheritdoc />
    public partial class MinorSchemaChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceToken",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Notifications");

            migrationBuilder.AddColumn<Dictionary<string, object>>(
                name: "Metadata",
                table: "Notifications",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Notifications");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Notifications",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);
        }
    }
}
