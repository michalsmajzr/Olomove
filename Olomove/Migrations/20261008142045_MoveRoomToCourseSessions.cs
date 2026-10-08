using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Olomove.Migrations
{
    /// <inheritdoc />
    public partial class MoveRoomToCourseSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Rooms_RoomId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_RoomId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Room",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "Courses");

            migrationBuilder.AddColumn<Guid>(
                name: "RoomId",
                table: "CourseSessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_RoomId",
                table: "CourseSessions",
                column: "RoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseSessions_Rooms_RoomId",
                table: "CourseSessions",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseSessions_Rooms_RoomId",
                table: "CourseSessions");

            migrationBuilder.DropIndex(
                name: "IX_CourseSessions_RoomId",
                table: "CourseSessions");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "CourseSessions");

            migrationBuilder.AddColumn<string>(
                name: "Room",
                table: "Courses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "RoomId",
                table: "Courses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_RoomId",
                table: "Courses",
                column: "RoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Rooms_RoomId",
                table: "Courses",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id");
        }
    }
}
