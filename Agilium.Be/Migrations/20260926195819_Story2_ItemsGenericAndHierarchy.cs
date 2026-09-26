using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eng.Agilium.Be.Migrations
{
    /// <inheritdoc />
    public partial class Story2_ItemsGenericAndHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGeneric",
                table: "Items",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGeneric",
                table: "Items");
        }
    }
}
