using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eng.Agilium.Be.Migrations
{
    /// <inheritdoc />
    public partial class Step5_EmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerifiedAt",
                table: "AppUsers",
                type: "datetime2",
                nullable: true);

            // accounts created before the verification existed are trusted
            migrationBuilder.Sql("UPDATE AppUsers SET EmailVerifiedAt = SYSUTCDATETIME() WHERE EmailVerifiedAt IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailVerifiedAt",
                table: "AppUsers");
        }
    }
}
