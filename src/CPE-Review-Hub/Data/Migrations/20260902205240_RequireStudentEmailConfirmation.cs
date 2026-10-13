using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPE_Review_Hub.Data.Migrations
{
    /// <inheritdoc />
    public partial class RequireStudentEmailConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Accounts created before email verification existed must remain usable.
            migrationBuilder.Sql("""
                UPDATE `AspNetUsers`
                SET `EmailConfirmed` = TRUE
                WHERE `AccountType` = 'Student';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Do not unconfirm existing accounts when rolling back this data migration.
        }
    }
}
