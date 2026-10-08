using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Olomove.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseDanceStyles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_DanceStyles_DanceStyleId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_DanceStyleId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DanceStyleId",
                table: "Courses");

            migrationBuilder.CreateTable(
                name: "CourseDanceStyles",
                columns: table => new
                {
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    DanceStyleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseDanceStyles", x => new { x.CourseId, x.DanceStyleId });
                    table.ForeignKey(
                        name: "FK_CourseDanceStyles_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseDanceStyles_DanceStyles_DanceStyleId",
                        column: x => x.DanceStyleId,
                        principalTable: "DanceStyles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseDanceStyles_DanceStyleId",
                table: "CourseDanceStyles",
                column: "DanceStyleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseDanceStyles");

            migrationBuilder.AddColumn<Guid>(
                name: "DanceStyleId",
                table: "Courses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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
    }
}
