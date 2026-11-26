using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eng.Agilium.Be.Migrations
{
    /// <inheritdoc />
    public partial class Step3_Invitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Memberships_ProjectId",
                table: "Memberships");

            migrationBuilder.CreateTable(
                name: "ProjectInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    KeyHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    KeyPrefix = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxUses = table.Column<int>(type: "int", nullable: false),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectInvitations_AppUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectInvitations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectInvitations_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_ProjectId_UserId",
                table: "Memberships",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_CreatedById",
                table: "ProjectInvitations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_KeyHash",
                table: "ProjectInvitations",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_ProjectId",
                table: "ProjectInvitations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectInvitations_RoleId",
                table: "ProjectInvitations",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectInvitations");

            migrationBuilder.DropIndex(
                name: "IX_Memberships_ProjectId_UserId",
                table: "Memberships");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_ProjectId",
                table: "Memberships",
                column: "ProjectId");
        }
    }
}
