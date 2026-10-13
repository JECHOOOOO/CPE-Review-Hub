using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPE_Review_Hub.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameAcademicLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AcademicLevels",
                keyColumn: "Id",
                keyValue: 200,
                column: "Name",
                value: "Sophomore");

            migrationBuilder.UpdateData(
                table: "AcademicLevels",
                keyColumn: "Id",
                keyValue: 300,
                column: "Name",
                value: "Junior");

            migrationBuilder.UpdateData(
                table: "AcademicLevels",
                keyColumn: "Id",
                keyValue: 400,
                column: "Name",
                value: "Senior");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AcademicLevels",
                keyColumn: "Id",
                keyValue: 200,
                column: "Name",
                value: "200 Level");

            migrationBuilder.UpdateData(
                table: "AcademicLevels",
                keyColumn: "Id",
                keyValue: 300,
                column: "Name",
                value: "300 Level");

            migrationBuilder.UpdateData(
                table: "AcademicLevels",
                keyColumn: "Id",
                keyValue: 400,
                column: "Name",
                value: "400 Level");
        }
    }
}
