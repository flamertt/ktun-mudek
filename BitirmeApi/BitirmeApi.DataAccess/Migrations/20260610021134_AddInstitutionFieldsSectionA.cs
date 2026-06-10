using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitirmeApi.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutionFieldsSectionA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CourseContent",
                table: "SemesterReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CourseCredit",
                table: "SemesterReports",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CourseEcts",
                table: "SemesterReports",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CourseObjective",
                table: "SemesterReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CoursePracticeHours",
                table: "SemesterReports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CourseSemester",
                table: "SemesterReports",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CourseTheoryHours",
                table: "SemesterReports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SectionGDiscussion",
                table: "SemesterReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SectionHCommentary",
                table: "SemesterReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SectionMImprovement",
                table: "SemesterReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExamTypeLabel",
                table: "SemesterReportFiles",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseContent",
                table: "SemesterReports");

            migrationBuilder.DropColumn(
                name: "CourseCredit",
                table: "SemesterReports");

            migrationBuilder.DropColumn(
                name: "CourseEcts",
                table: "SemesterReports");

            migrationBuilder.DropColumn(
                name: "CourseObjective",
                table: "SemesterReports");

            migrationBuilder.DropColumn(
                name: "CoursePracticeHours",
                table: "SemesterReports");

            migrationBuilder.DropColumn(
                name: "CourseSemester",
                table: "SemesterReports");

            migrationBuilder.DropColumn(
                name: "CourseTheoryHours",
                table: "SemesterReports");

            migrationBuilder.DropColumn(
                name: "SectionGDiscussion",
                table: "SemesterReports");

            migrationBuilder.DropColumn(
                name: "SectionHCommentary",
                table: "SemesterReports");

            migrationBuilder.DropColumn(
                name: "SectionMImprovement",
                table: "SemesterReports");

            migrationBuilder.DropColumn(
                name: "ExamTypeLabel",
                table: "SemesterReportFiles");
        }
    }
}
