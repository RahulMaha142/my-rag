using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagNotes.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentEmbeddingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentEmbeddingIndexes",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmbeddingModel = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ChunkCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IndexedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentEmbeddingIndexes", x => new { x.DocumentId, x.EmbeddingModel });
                    table.ForeignKey(
                        name: "FK_DocumentEmbeddingIndexes_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentEmbeddingIndexes");
        }
    }
}
