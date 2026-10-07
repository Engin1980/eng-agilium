using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eng.Agilium.Be.Migrations
{
    /// <inheritdoc />
    public partial class Story4_SprintIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sprints_ProjectId",
                table: "Sprints");

            migrationBuilder.DropIndex(
                name: "IX_SprintItems_ItemId",
                table: "SprintItems");

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_ProjectId_Title",
                table: "Sprints",
                columns: new[] { "ProjectId", "Title" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SprintItems_ItemId",
                table: "SprintItems",
                column: "ItemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sprints_ProjectId_Title",
                table: "Sprints");

            migrationBuilder.DropIndex(
                name: "IX_SprintItems_ItemId",
                table: "SprintItems");

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_ProjectId",
                table: "Sprints",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SprintItems_ItemId",
                table: "SprintItems",
                column: "ItemId");
        }
    }
}
