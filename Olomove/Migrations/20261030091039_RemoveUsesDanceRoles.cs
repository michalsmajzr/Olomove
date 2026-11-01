using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Olomove.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUsesDanceRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsesDanceRoles",
                table: "Courses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UsesDanceRoles",
                table: "Courses",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
