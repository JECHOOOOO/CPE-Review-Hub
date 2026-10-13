using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPE_Review_Hub.Data.Migrations
{
    /// <inheritdoc />
    public partial class FreshmanLevelAndCourseMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Semester",
                table: "Subjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Units",
                table: "Subjects",
                type: "decimal(4,1)",
                precision: 4,
                scale: 1,
                nullable: true);

            migrationBuilder.Sql("INSERT INTO `AcademicLevels` (`Id`, `Name`) VALUES (100, 'Freshman') ON DUPLICATE KEY UPDATE `Name` = 'Freshman'");

            // Add only the four requested course records. This is an idempotent
            // insert by unique course code and does not create placeholder
            // lessons, activities, quizzes, exams, or progress records.
            migrationBuilder.Sql("""
                INSERT INTO `Subjects`
                    (`AcademicLevelId`, `Code`, `Title`, `Description`, `Units`, `Semester`, `IsPublished`, `CreatedAtUtc`, `UpdatedAtUtc`)
                SELECT 100, 'CPE 101', 'Introduction to Computing', '', 3.0, 1, 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
                FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `Subjects` WHERE `Code` = 'CPE 101')
                """);
            migrationBuilder.Sql("""
                INSERT INTO `Subjects`
                    (`AcademicLevelId`, `Code`, `Title`, `Description`, `Units`, `Semester`, `IsPublished`, `CreatedAtUtc`, `UpdatedAtUtc`)
                SELECT 100, 'CPE 102', 'Computer Programming 1', '', 3.0, 1, 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
                FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `Subjects` WHERE `Code` = 'CPE 102')
                """);
            migrationBuilder.Sql("""
                INSERT INTO `Subjects`
                    (`AcademicLevelId`, `Code`, `Title`, `Description`, `Units`, `Semester`, `IsPublished`, `CreatedAtUtc`, `UpdatedAtUtc`)
                SELECT 100, 'CPE 103', 'Computer Programming 2', '', 3.0, 2, 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
                FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `Subjects` WHERE `Code` = 'CPE 103')
                """);
            migrationBuilder.Sql("""
                INSERT INTO `Subjects`
                    (`AcademicLevelId`, `Code`, `Title`, `Description`, `Units`, `Semester`, `IsPublished`, `CreatedAtUtc`, `UpdatedAtUtc`)
                SELECT 100, 'CPE 104', 'Discrete Mathematics', '', 3.0, 2, 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
                FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `Subjects` WHERE `Code` = 'CPE 104')
                """);

            // If any of the course codes already exist, preserve their primary
            // keys, lessons, and student history while correcting only the
            // requested course metadata.
            migrationBuilder.Sql("UPDATE `Subjects` SET `AcademicLevelId` = 100, `Title` = 'Introduction to Computing', `Units` = 3.0, `Semester` = 1 WHERE `Code` = 'CPE 101'");
            migrationBuilder.Sql("UPDATE `Subjects` SET `AcademicLevelId` = 100, `Title` = 'Computer Programming 1', `Units` = 3.0, `Semester` = 1 WHERE `Code` = 'CPE 102'");
            migrationBuilder.Sql("UPDATE `Subjects` SET `AcademicLevelId` = 100, `Title` = 'Computer Programming 2', `Units` = 3.0, `Semester` = 2 WHERE `Code` = 'CPE 103'");
            migrationBuilder.Sql("UPDATE `Subjects` SET `AcademicLevelId` = 100, `Title` = 'Discrete Mathematics', `Units` = 3.0, `Semester` = 2 WHERE `Code` = 'CPE 104'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep the Freshman level and course records on rollback if they
            // have been used. Avoid cascading into lessons or student history.
            migrationBuilder.Sql("DELETE FROM `AcademicLevels` WHERE `Id` = 100 AND NOT EXISTS (SELECT 1 FROM `Subjects` WHERE `AcademicLevelId` = 100)");

            migrationBuilder.DropColumn(
                name: "Semester",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Units",
                table: "Subjects");
        }
    }
}
