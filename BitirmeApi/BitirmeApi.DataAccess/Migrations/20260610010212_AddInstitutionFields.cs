using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitirmeApi.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DepartmentName",
                table: "SemesterReports",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacultyName",
                table: "SemesterReports",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UniversityName",
                table: "SemesterReports",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartmentName",
                table: "SemesterReports");

            migrationBuilder.DropColumn(
                name: "FacultyName",
                table: "SemesterReports");

            migrationBuilder.DropColumn(
                name: "UniversityName",
                table: "SemesterReports");
        }
    }
}
