using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace CPE_Review_Hub.Data.Migrations
{
    /// <inheritdoc />
    public partial class LessonImagesAndActivityTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Options",
                table: "LessonActivities",
                type: "varchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ResponseType",
                table: "LessonActivities",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "LessonImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    LessonId = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    AltText = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Caption = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonImages_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LessonImages_LessonId_DisplayOrder",
                table: "LessonImages",
                columns: new[] { "LessonId", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonImages");

            migrationBuilder.DropColumn(
                name: "Options",
                table: "LessonActivities");

            migrationBuilder.DropColumn(
                name: "ResponseType",
                table: "LessonActivities");
        }
    }
}
