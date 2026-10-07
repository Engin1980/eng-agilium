using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eng.Agilium.Be.Migrations
{
    /// <inheritdoc />
    public partial class Story3X_TemplateTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Old CSS-grid templates (and the values tied to their fields) cannot be converted - they are dropped;
            // global templates are re-seeded and project templates re-copied from them on startup.
            migrationBuilder.Sql("DELETE FROM ItemFieldValues");
            migrationBuilder.Sql("DELETE FROM TemplateItems");
            migrationBuilder.Sql("DELETE FROM Templates");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateItems_Templates_TemplateId",
                table: "TemplateItems");

            migrationBuilder.DropIndex(
                name: "IX_TemplateItems_TemplateId_Key",
                table: "TemplateItems");

            migrationBuilder.DropColumn(
                name: "ColumnCount",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "ColumnSpan",
                table: "TemplateItems");

            migrationBuilder.DropColumn(
                name: "ColumnStart",
                table: "TemplateItems");

            migrationBuilder.DropColumn(
                name: "RowSpan",
                table: "TemplateItems");

            migrationBuilder.DropColumn(
                name: "RowStart",
                table: "TemplateItems");

            migrationBuilder.RenameColumn(
                name: "TemplateId",
                table: "TemplateItems",
                newName: "TemplateSectionId");

            migrationBuilder.CreateTable(
                name: "TemplateTables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateTables_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateTableId = table.Column<int>(type: "int", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateColumns_TemplateTables_TemplateTableId",
                        column: x => x.TemplateTableId,
                        principalTable: "TemplateTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateColumnId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateSections_TemplateColumns_TemplateColumnId",
                        column: x => x.TemplateColumnId,
                        principalTable: "TemplateColumns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateItems_TemplateSectionId",
                table: "TemplateItems",
                column: "TemplateSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateColumns_TemplateTableId",
                table: "TemplateColumns",
                column: "TemplateTableId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSections_TemplateColumnId",
                table: "TemplateSections",
                column: "TemplateColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateTables_TemplateId",
                table: "TemplateTables",
                column: "TemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateItems_TemplateSections_TemplateSectionId",
                table: "TemplateItems",
                column: "TemplateSectionId",
                principalTable: "TemplateSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemplateItems_TemplateSections_TemplateSectionId",
                table: "TemplateItems");

            migrationBuilder.DropTable(
                name: "TemplateSections");

            migrationBuilder.DropTable(
                name: "TemplateColumns");

            migrationBuilder.DropTable(
                name: "TemplateTables");

            migrationBuilder.DropIndex(
                name: "IX_TemplateItems_TemplateSectionId",
                table: "TemplateItems");

            migrationBuilder.RenameColumn(
                name: "TemplateSectionId",
                table: "TemplateItems",
                newName: "TemplateId");

            migrationBuilder.AddColumn<int>(
                name: "ColumnCount",
                table: "Templates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ColumnSpan",
                table: "TemplateItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ColumnStart",
                table: "TemplateItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RowSpan",
                table: "TemplateItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RowStart",
                table: "TemplateItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateItems_TemplateId_Key",
                table: "TemplateItems",
                columns: new[] { "TemplateId", "Key" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateItems_Templates_TemplateId",
                table: "TemplateItems",
                column: "TemplateId",
                principalTable: "Templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
