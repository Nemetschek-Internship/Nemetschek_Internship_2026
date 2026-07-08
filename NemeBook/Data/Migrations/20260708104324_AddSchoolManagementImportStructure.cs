using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolManagementImportStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Teachers_MainTeacherId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassSubjects_Teachers_TeacherId",
                table: "ClassSubjects");

            migrationBuilder.DropIndex(
                name: "IX_Classes_MainTeacherId",
                table: "Classes");

            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                table: "Teachers",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AlterColumn<Guid>(
                name: "TeacherId",
                table: "ClassSubjects",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "MainTeacherId",
                table: "Classes",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateTable(
                name: "ClassScheduleEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassSubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    PeriodNumber = table.Column<int>(type: "int", nullable: false),
                    StartsAt = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndsAt = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassScheduleEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassScheduleEntries_ClassSubjects_ClassSubjectId",
                        column: x => x.ClassSubjectId,
                        principalTable: "ClassSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassScheduleEntries_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeacherSubjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherSubjects_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherSubjects_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Classes_MainTeacherId",
                table: "Classes",
                column: "MainTeacherId",
                unique: true,
                filter: "[MainTeacherId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClassScheduleEntries_ClassId_DayOfWeek_PeriodNumber",
                table: "ClassScheduleEntries",
                columns: new[] { "ClassId", "DayOfWeek", "PeriodNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassScheduleEntries_ClassSubjectId",
                table: "ClassScheduleEntries",
                column: "ClassSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_SubjectId",
                table: "TeacherSubjects",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_TeacherId_SubjectId",
                table: "TeacherSubjects",
                columns: new[] { "TeacherId", "SubjectId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Teachers_MainTeacherId",
                table: "Classes",
                column: "MainTeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSubjects_Teachers_TeacherId",
                table: "ClassSubjects",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Teachers_MainTeacherId",
                table: "Classes");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassSubjects_Teachers_TeacherId",
                table: "ClassSubjects");

            migrationBuilder.DropTable(
                name: "ClassScheduleEntries");

            migrationBuilder.DropTable(
                name: "TeacherSubjects");

            migrationBuilder.DropIndex(
                name: "IX_Classes_MainTeacherId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "Teachers");

            migrationBuilder.AlterColumn<Guid>(
                name: "TeacherId",
                table: "ClassSubjects",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "MainTeacherId",
                table: "Classes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_MainTeacherId",
                table: "Classes",
                column: "MainTeacherId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Teachers_MainTeacherId",
                table: "Classes",
                column: "MainTeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassSubjects_Teachers_TeacherId",
                table: "ClassSubjects",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
