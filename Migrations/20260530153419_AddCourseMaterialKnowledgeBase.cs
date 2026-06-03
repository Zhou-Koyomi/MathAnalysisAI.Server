using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathAnalysisAI.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseMaterialKnowledgeBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseMaterials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MaterialKind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Author = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Edition = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Publisher = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Visibility = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CopyrightNote = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    StoragePath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ParseStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ParseMessage = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ParsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseMaterials_AppUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CourseMaterials_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialChunks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseMaterialId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: true),
                    ChunkIndex = table.Column<int>(type: "int", nullable: false),
                    ChunkType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SemanticTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SectionTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SectionPath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PageStart = table.Column<int>(type: "int", nullable: true),
                    PageEnd = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentPreview = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FormulaText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedFormulaText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenCountEstimate = table.Column<int>(type: "int", nullable: false),
                    StartOffset = table.Column<int>(type: "int", nullable: true),
                    EndOffset = table.Column<int>(type: "int", nullable: true),
                    DifficultyLevel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialChunks_AppUsers_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MaterialChunks_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MaterialChunks_CourseMaterials_CourseMaterialId",
                        column: x => x.CourseMaterialId,
                        principalTable: "CourseMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialChunks_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaterialChunkKnowledgePoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaterialChunkId = table.Column<int>(type: "int", nullable: false),
                    KnowledgePointId = table.Column<int>(type: "int", nullable: false),
                    RelationType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialChunkKnowledgePoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialChunkKnowledgePoints_KnowledgePoints_KnowledgePointId",
                        column: x => x.KnowledgePointId,
                        principalTable: "KnowledgePoints",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaterialChunkKnowledgePoints_MaterialChunks_MaterialChunkId",
                        column: x => x.MaterialChunkId,
                        principalTable: "MaterialChunks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterials_CourseId",
                table: "CourseMaterials",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterials_CourseId_FileHash",
                table: "CourseMaterials",
                columns: new[] { "CourseId", "FileHash" },
                unique: true,
                filter: "[FileHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterials_CourseId_MaterialKind_Visibility",
                table: "CourseMaterials",
                columns: new[] { "CourseId", "MaterialKind", "Visibility" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterials_ParseStatus",
                table: "CourseMaterials",
                column: "ParseStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterials_UploadedAt",
                table: "CourseMaterials",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMaterials_UploadedByUserId",
                table: "CourseMaterials",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialChunkKnowledgePoints_KnowledgePointId_RelationType",
                table: "MaterialChunkKnowledgePoints",
                columns: new[] { "KnowledgePointId", "RelationType" });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialChunkKnowledgePoints_MaterialChunkId_KnowledgePointId_RelationType",
                table: "MaterialChunkKnowledgePoints",
                columns: new[] { "MaterialChunkId", "KnowledgePointId", "RelationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialChunks_ChapterId",
                table: "MaterialChunks",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialChunks_CourseId_ChapterId_ChunkType",
                table: "MaterialChunks",
                columns: new[] { "CourseId", "ChapterId", "ChunkType" });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialChunks_CourseMaterialId_ChunkIndex",
                table: "MaterialChunks",
                columns: new[] { "CourseMaterialId", "ChunkIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialChunks_IsVerified_ChunkType",
                table: "MaterialChunks",
                columns: new[] { "IsVerified", "ChunkType" });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialChunks_VerifiedByUserId_VerifiedAt",
                table: "MaterialChunks",
                columns: new[] { "VerifiedByUserId", "VerifiedAt" });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialChunkKnowledgePoints");

            migrationBuilder.DropTable(
                name: "MaterialChunks");

            migrationBuilder.DropTable(
                name: "CourseMaterials");
        }
    }
}
