using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eng.Agilium.Be.Migrations
{
    /// <inheritdoc />
    public partial class Story3_TemplateGridAndFieldValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemplateItems_TemplateColumns_TemplateColumnId",
                table: "TemplateItems");

            migrationBuilder.DropTable(
                name: "TemplateColumns");

            migrationBuilder.DropIndex(
                name: "IX_Templates_ProjectId",
                table: "Templates");

            migrationBuilder.DropIndex(
                name: "IX_TemplateItems_TemplateColumnId",
                table: "TemplateItems");

            migrationBuilder.RenameColumn(
                name: "TemplateColumnId",
                table: "TemplateItems",
                newName: "TemplateId");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "Templates",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ColumnCount",
                table: "Templates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "TemplateItems",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

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

            migrationBuilder.CreateTable(
                name: "ItemFieldValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    TemplateItemId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemFieldValues_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemFieldValues_TemplateItems_TemplateItemId",
                        column: x => x.TemplateItemId,
                        principalTable: "TemplateItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Templates_ProjectId_Type",
                table: "Templates",
                columns: new[] { "ProjectId", "Type" },
                unique: true,
                filter: "[ProjectId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_Type",
                table: "Templates",
                column: "Type",
                unique: true,
                filter: "[ProjectId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateItems_TemplateId_Key",
                table: "TemplateItems",
                columns: new[] { "TemplateId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemFieldValues_ItemId_TemplateItemId",
                table: "ItemFieldValues",
                columns: new[] { "ItemId", "TemplateItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemFieldValues_TemplateItemId",
                table: "ItemFieldValues",
                column: "TemplateItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateItems_Templates_TemplateId",
                table: "TemplateItems",
                column: "TemplateId",
                principalTable: "Templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemplateItems_Templates_TemplateId",
                table: "TemplateItems");

            migrationBuilder.DropTable(
                name: "ItemFieldValues");

            migrationBuilder.DropIndex(
                name: "IX_Templates_ProjectId_Type",
                table: "Templates");

            migrationBuilder.DropIndex(
                name: "IX_Templates_Type",
                table: "Templates");

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
                newName: "TemplateColumnId");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "Templates",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "TemplateItems",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

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

            migrationBuilder.CreateIndex(
                name: "IX_Templates_ProjectId",
                table: "Templates",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateItems_TemplateColumnId",
                table: "TemplateItems",
                column: "TemplateColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateColumns_TemplateId",
                table: "TemplateColumns",
                column: "TemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateItems_TemplateColumns_TemplateColumnId",
                table: "TemplateItems",
                column: "TemplateColumnId",
                principalTable: "TemplateColumns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
