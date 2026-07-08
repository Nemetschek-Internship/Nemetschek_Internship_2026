using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonScheduleLinksToRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Grades",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Absences",
                newName: "ExcuseNote");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Grades",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "Date",
                table: "Feedbacks",
                type: "date",
                nullable: false,
                defaultValueSql: "CONVERT(date, GETUTCDATE())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<Guid>(
                name: "ClassScheduleEntryId",
                table: "Feedbacks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Feedbacks",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<Guid>(
                name: "ClassSubjectId",
                table: "Events",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "Date",
                table: "Absences",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<Guid>(
                name: "ClassScheduleEntryId",
                table: "Absences",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Absences",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_ClassScheduleEntryId",
                table: "Feedbacks",
                column: "ClassScheduleEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_ClassSubjectId",
                table: "Events",
                column: "ClassSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Absences_ClassScheduleEntryId",
                table: "Absences",
                column: "ClassScheduleEntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Absences_ClassScheduleEntries_ClassScheduleEntryId",
                table: "Absences",
                column: "ClassScheduleEntryId",
                principalTable: "ClassScheduleEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_ClassSubjects_ClassSubjectId",
                table: "Events",
                column: "ClassSubjectId",
                principalTable: "ClassSubjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_ClassScheduleEntries_ClassScheduleEntryId",
                table: "Feedbacks",
                column: "ClassScheduleEntryId",
                principalTable: "ClassScheduleEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Absences_ClassScheduleEntries_ClassScheduleEntryId",
                table: "Absences");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_ClassSubjects_ClassSubjectId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_ClassScheduleEntries_ClassScheduleEntryId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_ClassScheduleEntryId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Events_ClassSubjectId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Absences_ClassScheduleEntryId",
                table: "Absences");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Grades",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.DropColumn(
                name: "ClassScheduleEntryId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "ClassSubjectId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ClassScheduleEntryId",
                table: "Absences");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Absences");

            migrationBuilder.RenameColumn(
                name: "ExcuseNote",
                table: "Absences",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Grades",
                newName: "Date");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "Feedbacks",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldDefaultValueSql: "CONVERT(date, GETUTCDATE())");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "Absences",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");
        }
    }
}
