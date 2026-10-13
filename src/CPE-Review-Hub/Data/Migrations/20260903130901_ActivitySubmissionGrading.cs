using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CPE_Review_Hub.Data.Migrations
{
    /// <inheritdoc />
    public partial class ActivitySubmissionGrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "ActivitySubmissions",
                type: "varchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Grade",
                table: "ActivitySubmissions",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GradedAtUtc",
                table: "ActivitySubmissions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GradedById",
                table: "ActivitySubmissions",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "ActivitySubmissions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ActivitySubmissions_GradedById",
                table: "ActivitySubmissions",
                column: "GradedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivitySubmissions_AspNetUsers_GradedById",
                table: "ActivitySubmissions",
                column: "GradedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivitySubmissions_AspNetUsers_GradedById",
                table: "ActivitySubmissions");

            migrationBuilder.DropIndex(
                name: "IX_ActivitySubmissions_GradedById",
                table: "ActivitySubmissions");

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "ActivitySubmissions");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "ActivitySubmissions");

            migrationBuilder.DropColumn(
                name: "GradedAtUtc",
                table: "ActivitySubmissions");

            migrationBuilder.DropColumn(
                name: "GradedById",
                table: "ActivitySubmissions");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "ActivitySubmissions");
        }
    }
}
