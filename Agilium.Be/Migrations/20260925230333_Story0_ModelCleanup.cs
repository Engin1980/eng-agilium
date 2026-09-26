using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eng.Agilium.Be.Migrations
{
    /// <inheritdoc />
    public partial class Story0_ModelCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedEndDateTime",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "ExpectedStartDateTime",
                table: "Sprints");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Projects",
                newName: "State");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "State",
                table: "Projects",
                newName: "Status");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedEndDateTime",
                table: "Sprints",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedStartDateTime",
                table: "Sprints",
                type: "datetime2",
                nullable: true);
        }
    }
}
