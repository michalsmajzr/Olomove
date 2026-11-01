using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Olomove.Migrations
{
    /// <inheritdoc />
    public partial class EditSavedPartner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseEnrollmentPartners");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "CourseEnrollments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "DanceRole",
                table: "CourseEnrollments",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CourseId1",
                table: "CourseEnrollments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartnerDanceRole",
                table: "CourseEnrollments",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PartnerRotation",
                table: "CourseEnrollments",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SavedPartnerId",
                table: "CourseEnrollments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_CourseId1",
                table: "CourseEnrollments",
                column: "CourseId1");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_SavedPartnerId",
                table: "CourseEnrollments",
                column: "SavedPartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollments_Courses_CourseId1",
                table: "CourseEnrollments",
                column: "CourseId1",
                principalTable: "Courses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollments_SavedPartners_SavedPartnerId",
                table: "CourseEnrollments",
                column: "SavedPartnerId",
                principalTable: "SavedPartners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollments_Courses_CourseId1",
                table: "CourseEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollments_SavedPartners_SavedPartnerId",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_CourseId1",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_SavedPartnerId",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "CourseId1",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "PartnerDanceRole",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "PartnerRotation",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "SavedPartnerId",
                table: "CourseEnrollments");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "CourseEnrollments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "DanceRole",
                table: "CourseEnrollments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "CourseEnrollmentPartners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SavedPartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DanceRole = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
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
    }
}
