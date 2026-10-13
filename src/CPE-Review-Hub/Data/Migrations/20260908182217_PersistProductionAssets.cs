using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace CPE_Review_Hub.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersistProductionAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "LessonImages",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "LessonImages",
                type: "longblob",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    FriendlyName = table.Column<string>(type: "longtext", nullable: true),
                    Xml = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "LessonImages");

            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "LessonImages");
        }
    }
}
