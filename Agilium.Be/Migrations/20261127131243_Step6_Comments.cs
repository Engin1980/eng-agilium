using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eng.Agilium.Be.Migrations
{
    /// <inheritdoc />
    public partial class Step6_Comments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    TemplateItemId = table.Column<int>(type: "int", nullable: false),
                    AuthorId = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemComments_AppUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemComments_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemComments_TemplateItems_TemplateItemId",
                        column: x => x.TemplateItemId,
                        principalTable: "TemplateItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemComments_AuthorId",
                table: "ItemComments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemComments_ItemId_TemplateItemId",
                table: "ItemComments",
                columns: new[] { "ItemId", "TemplateItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemComments_TemplateItemId",
                table: "ItemComments",
                column: "TemplateItemId");

            // Texts typed into Comments elements before comments became a list: keep each one as the first comment
            // (authored by an owner of the item's project) and remove the old value.
            migrationBuilder.Sql(@"
INSERT INTO ItemComments (ItemId, TemplateItemId, AuthorId, [Text], CreatedAt)
SELECT v.ItemId, v.TemplateItemId,
       (SELECT MIN(m.UserId) FROM Memberships m JOIN Roles r ON r.Id = m.RoleId
         WHERE m.ProjectId = i.ProjectId AND r.SystemKind = 1),
       v.[Value], SYSUTCDATETIME()
FROM ItemFieldValues v
JOIN TemplateItems ti ON ti.Id = v.TemplateItemId
JOIN Items i ON i.Id = v.ItemId
WHERE ti.[Type] = 6 AND v.[Value] IS NOT NULL AND LTRIM(RTRIM(v.[Value])) <> ''
  AND EXISTS (SELECT 1 FROM Memberships m JOIN Roles r ON r.Id = m.RoleId WHERE m.ProjectId = i.ProjectId AND r.SystemKind = 1);

DELETE v FROM ItemFieldValues v JOIN TemplateItems ti ON ti.Id = v.TemplateItemId WHERE ti.[Type] = 6;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemComments");
        }
    }
}
