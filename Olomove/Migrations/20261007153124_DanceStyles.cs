using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Olomove.Migrations
{
    /// <inheritdoc />
    public partial class DanceStyles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DanceStyle",
                table: "Courses");

            migrationBuilder.AddColumn<Guid>(
                name: "DanceStyleId",
                table: "Courses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "DanceStyles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanceStyles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_DanceStyleId",
                table: "Courses",
                column: "DanceStyleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_DanceStyles_DanceStyleId",
                table: "Courses",
                column: "DanceStyleId",
                principalTable: "DanceStyles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_DanceStyles_DanceStyleId",
                table: "Courses");

            migrationBuilder.DropTable(
                name: "DanceStyles");

            migrationBuilder.DropIndex(
                name: "IX_Courses_DanceStyleId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DanceStyleId",
                table: "Courses");

            migrationBuilder.AddColumn<string>(
                name: "DanceStyle",
                table: "Courses",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
