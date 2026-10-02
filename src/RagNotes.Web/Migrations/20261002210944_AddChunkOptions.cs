using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagNotes.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddChunkOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxTokens",
                table: "Documents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 256);

            migrationBuilder.AddColumn<int>(
                name: "OverlapPercent",
                table: "Documents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 20);

            migrationBuilder.AddColumn<bool>(
                name: "PreferSentences",
                table: "Documents",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxTokens",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "OverlapPercent",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "PreferSentences",
                table: "Documents");
        }
    }
}
