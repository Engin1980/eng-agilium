using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eng.Agilium.Be.Migrations
{
    /// <inheritdoc />
    public partial class Step2_RoleSystemKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Roles_ProjectId",
                table: "Roles");

            migrationBuilder.AddColumn<bool>(
                name: "CanEditItems",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SystemKind",
                table: "Roles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_ProjectId_Title",
                table: "Roles",
                columns: new[] { "ProjectId", "Title" },
                unique: true);

            // Backfill of existing projects: the former Owner/Guest roles become system roles,
            // and every project gets the new Manager and Member roles (see .docs/development/roles.md).
            migrationBuilder.Sql("UPDATE Roles SET SystemKind = 1, CanEditItems = 1, CanViewProject = 1, CanViewMembers = 1, CanManageSprints = 1, CanManageProject = 1, CanManageMembers = 1 WHERE Title = 'Owner'");
            migrationBuilder.Sql("UPDATE Roles SET SystemKind = 4 WHERE Title = 'Guest'");
            migrationBuilder.Sql(@"INSERT INTO Roles (Title, ProjectId, SystemKind, CanViewProject, CanManageProject, CanViewMembers, CanManageMembers, CanManageSprints, CanEditItems)
SELECT 'Manager', p.Id, 2, 1, 1, 1, 1, 1, 1 FROM Projects p WHERE NOT EXISTS (SELECT 1 FROM Roles r WHERE r.ProjectId = p.Id AND r.Title = 'Manager')");
            migrationBuilder.Sql(@"INSERT INTO Roles (Title, ProjectId, SystemKind, CanViewProject, CanManageProject, CanViewMembers, CanManageMembers, CanManageSprints, CanEditItems)
SELECT 'Member', p.Id, 3, 1, 0, 1, 0, 0, 1 FROM Projects p WHERE NOT EXISTS (SELECT 1 FROM Roles r WHERE r.ProjectId = p.Id AND r.Title = 'Member')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Roles_ProjectId_Title",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CanEditItems",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "SystemKind",
                table: "Roles");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_ProjectId",
                table: "Roles",
                column: "ProjectId");
        }
    }
}
