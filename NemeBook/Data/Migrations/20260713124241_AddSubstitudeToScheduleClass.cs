using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubstitudeToScheduleClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SubstituteTeacherId",
                table: "ClassScheduleEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassScheduleEntries_SubstituteTeacherId",
                table: "ClassScheduleEntries",
                column: "SubstituteTeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassScheduleEntries_Teachers_SubstituteTeacherId",
                table: "ClassScheduleEntries",
                column: "SubstituteTeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassScheduleEntries_Teachers_SubstituteTeacherId",
                table: "ClassScheduleEntries");

            migrationBuilder.DropIndex(
                name: "IX_ClassScheduleEntries_SubstituteTeacherId",
                table: "ClassScheduleEntries");

            migrationBuilder.DropColumn(
                name: "SubstituteTeacherId",
                table: "ClassScheduleEntries");
        }
    }
}
