using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eng.Agilium.Be.Migrations
{
  /// <inheritdoc />
  public partial class Add_TemplateSupport : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "Templates",
          columns: table => new
          {
            Id = table.Column<int>(type: "int", nullable: false)
                  .Annotation("SqlServer:Identity", "1, 1"),
            ProjectId = table.Column<int>(type: "int", nullable: false),
            Type = table.Column<int>(type: "int", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_Templates", x => x.Id);
            table.ForeignKey(
                      name: "FK_Templates_Projects_ProjectId",
                      column: x => x.ProjectId,
                      principalTable: "Projects",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateTable(
          name: "TemplateColumns",
          columns: table => new
          {
            Id = table.Column<int>(type: "int", nullable: false)
                  .Annotation("SqlServer:Identity", "1, 1"),
            TemplateId = table.Column<int>(type: "int", nullable: false),
            WidthWeight = table.Column<int>(type: "int", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_TemplateColumns", x => x.Id);
            table.ForeignKey(
                      name: "FK_TemplateColumns_Templates_TemplateId",
                      column: x => x.TemplateId,
                      principalTable: "Templates",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateTable(
          name: "TemplateItems",
          columns: table => new
          {
            Id = table.Column<int>(type: "int", nullable: false)
                  .Annotation("SqlServer:Identity", "1, 1"),
            OrderIndex = table.Column<int>(type: "int", nullable: false),
            TemplateColumnId = table.Column<int>(type: "int", nullable: false),
            Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
            Type = table.Column<int>(type: "int", nullable: false),
            ValidatingRegex = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
            ColumnIndex = table.Column<int>(type: "int", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_TemplateItems", x => x.Id);
            table.ForeignKey(
                      name: "FK_TemplateItems_TemplateColumns_TemplateColumnId",
                      column: x => x.TemplateColumnId,
                      principalTable: "TemplateColumns",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateIndex(
          name: "IX_TemplateColumns_TemplateId",
          table: "TemplateColumns",
          column: "TemplateId");

      migrationBuilder.CreateIndex(
          name: "IX_TemplateItems_TemplateColumnId",
          table: "TemplateItems",
          column: "TemplateColumnId");

      migrationBuilder.CreateIndex(
          name: "IX_Templates_ProjectId",
          table: "Templates",
          column: "ProjectId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "TemplateItems");

      migrationBuilder.DropTable(
          name: "TemplateColumns");

      migrationBuilder.DropTable(
          name: "Templates");
    }
  }
}
