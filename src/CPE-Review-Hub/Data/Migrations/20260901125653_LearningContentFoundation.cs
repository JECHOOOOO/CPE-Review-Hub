using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CPE_Review_Hub.Data.Migrations
{
    /// <inheritdoc />
    public partial class LearningContentFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicLevels", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AcademicLevelId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    IsPublished = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subjects_AcademicLevels_AcademicLevelId",
                        column: x => x.AcademicLevelId,
                        principalTable: "AcademicLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lessons_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReviewMaterials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    LessonId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "longtext", nullable: false),
                    ResourceUrl = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewMaterials_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "AcademicLevels",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 200, "200 Level" },
                    { 300, "300 Level" },
                    { 400, "400 Level" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_SubjectId_SortOrder",
                table: "Lessons",
                columns: new[] { "SubjectId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewMaterials_LessonId_SortOrder",
                table: "ReviewMaterials",
                columns: new[] { "LessonId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_AcademicLevelId_IsPublished",
                table: "Subjects",
                columns: new[] { "AcademicLevelId", "IsPublished" });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Code",
                table: "Subjects",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReviewMaterials");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "AcademicLevels");
        }
    }
}
