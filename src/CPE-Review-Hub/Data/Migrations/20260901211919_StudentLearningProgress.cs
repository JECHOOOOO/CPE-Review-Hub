using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace CPE_Review_Hub.Data.Migrations
{
    /// <inheritdoc />
    public partial class StudentLearningProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceKey",
                table: "Questions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActivitySubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    LessonActivityId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    Response = table.Column<string>(type: "longtext", maxLength: 10000, nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivitySubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivitySubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivitySubmissions_LessonActivities_LessonActivityId",
                        column: x => x.LessonActivityId,
                        principalTable: "LessonActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AssessmentAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AssessmentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Score = table.Column<int>(type: "int", nullable: false),
                    TotalQuestions = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsPassed = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentAttempts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentAttempts_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AttemptAnswers",
                columns: table => new
                {
                    AssessmentAttemptId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    ChoiceId = table.Column<int>(type: "int", nullable: false),
                    IsCorrect = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttemptAnswers", x => new { x.AssessmentAttemptId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_AttemptAnswers_AssessmentAttempts_AssessmentAttemptId",
                        column: x => x.AssessmentAttemptId,
                        principalTable: "AssessmentAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttemptAnswers_Choices_ChoiceId",
                        column: x => x.ChoiceId,
                        principalTable: "Choices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttemptAnswers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_SourceKey",
                table: "Questions",
                column: "SourceKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivitySubmissions_LessonActivityId",
                table: "ActivitySubmissions",
                column: "LessonActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivitySubmissions_UserId_LessonActivityId",
                table: "ActivitySubmissions",
                columns: new[] { "UserId", "LessonActivityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttempts_AssessmentId",
                table: "AssessmentAttempts",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentAttempts_UserId_AssessmentId_SubmittedAtUtc",
                table: "AssessmentAttempts",
                columns: new[] { "UserId", "AssessmentId", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AttemptAnswers_ChoiceId",
                table: "AttemptAnswers",
                column: "ChoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptAnswers_QuestionId",
                table: "AttemptAnswers",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivitySubmissions");

            migrationBuilder.DropTable(
                name: "AttemptAnswers");

            migrationBuilder.DropTable(
                name: "AssessmentAttempts");

            migrationBuilder.DropIndex(
                name: "IX_Questions_SourceKey",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "SourceKey",
                table: "Questions");
        }
    }
}
