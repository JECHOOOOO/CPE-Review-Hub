using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPE_Review_Hub.Data.Migrations
{
    /// <inheritdoc />
    public partial class OnlineLessonImageReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Author",
                table: "LessonImages",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "License",
                table: "LessonImages",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourceName",
                table: "LessonImages",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "LessonImages",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Author",
                table: "LessonImages");

            migrationBuilder.DropColumn(
                name: "License",
                table: "LessonImages");

            migrationBuilder.DropColumn(
                name: "SourceName",
                table: "LessonImages");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "LessonImages");
        }
    }
}
