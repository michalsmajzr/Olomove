using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Olomove.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseEnrollmentPartnersAndCredits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UsesDanceRoles",
                table: "Courses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidCredits",
                table: "CourseEnrollments",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CourseEnrollmentPartners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SavedPartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DanceRole = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Rotation = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseEnrollmentPartners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseEnrollmentPartners_CourseEnrollments_CourseEnrollment~",
                        column: x => x.CourseEnrollmentId,
                        principalTable: "CourseEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseEnrollmentPartners_SavedPartners_SavedPartnerId",
                        column: x => x.SavedPartnerId,
                        principalTable: "SavedPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollmentPartners_CourseEnrollmentId",
                table: "CourseEnrollmentPartners",
                column: "CourseEnrollmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollmentPartners_SavedPartnerId",
                table: "CourseEnrollmentPartners",
                column: "SavedPartnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseEnrollmentPartners");

            migrationBuilder.DropColumn(
                name: "UsesDanceRoles",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "PaidCredits",
                table: "CourseEnrollments");
        }
    }
}
