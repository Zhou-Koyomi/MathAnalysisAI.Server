using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathAnalysisAI.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningPlatformSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RealName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StudentNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SchoolName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DepartmentName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ClassName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chapters_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromptProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserPromptTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutputSchemaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromptProfiles_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCourseStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    CorrectCount = table.Column<int>(type: "int", nullable: false),
                    WrongCount = table.Column<int>(type: "int", nullable: false),
                    AccuracyRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    RankingScore = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCourseStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCourseStats_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCourseStats_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgePoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgePoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgePoints_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KnowledgePoints_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Problems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContentMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentLatex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceFilePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ProblemType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Difficulty = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Problems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Problems_AppUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Problems_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Problems_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeDependencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromKnowledgePointId = table.Column<int>(type: "int", nullable: false),
                    ToKnowledgePointId = table.Column<int>(type: "int", nullable: false),
                    DependencyType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeDependencies_KnowledgePoints_FromKnowledgePointId",
                        column: x => x.FromKnowledgePointId,
                        principalTable: "KnowledgePoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeDependencies_KnowledgePoints_ToKnowledgePointId",
                        column: x => x.ToKnowledgePointId,
                        principalTable: "KnowledgePoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserKnowledgeStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    KnowledgePointId = table.Column<int>(type: "int", nullable: false),
                    MasteryLevel = table.Column<int>(type: "int", nullable: false),
                    PracticeCount = table.Column<int>(type: "int", nullable: false),
                    CorrectCount = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserKnowledgeStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserKnowledgeStates_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserKnowledgeStates_KnowledgePoints_KnowledgePointId",
                        column: x => x.KnowledgePointId,
                        principalTable: "KnowledgePoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentSolutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProblemId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    SolutionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSolutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentSolutions_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudentSolutions_Problems_ProblemId",
                        column: x => x.ProblemId,
                        principalTable: "Problems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProblemId = table.Column<int>(type: "int", nullable: false),
                    StudentSolutionId = table.Column<int>(type: "int", nullable: true),
                    AnalysisMode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ChapterName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ProblemType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Difficulty = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    KnowledgePointsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StandardSolution = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudentSolutionReview = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MistakeTagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewSuggestionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiJudgedCorrect = table.Column<bool>(type: "bit", nullable: true),
                    FinalCorrect = table.Column<bool>(type: "bit", nullable: true),
                    FinalCorrectSource = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisResults_Problems_ProblemId",
                        column: x => x.ProblemId,
                        principalTable: "Problems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnalysisResults_StudentSolutions_StudentSolutionId",
                        column: x => x.StudentSolutionId,
                        principalTable: "StudentSolutions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AnalysisVisualizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnalysisResultId = table.Column<int>(type: "int", nullable: false),
                    Engine = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    VisualizationType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CommandsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ViewConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StepBindingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Caption = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ValidationStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ValidationMessage = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisVisualizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisVisualizations_AnalysisResults_AnalysisResultId",
                        column: x => x.AnalysisResultId,
                        principalTable: "AnalysisResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LLMRequestLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    AnalysisResultId = table.Column<int>(type: "int", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RequestType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PromptTokenCount = table.Column<int>(type: "int", nullable: true),
                    CompletionTokenCount = table.Column<int>(type: "int", nullable: true),
                    TotalTokenCount = table.Column<int>(type: "int", nullable: true),
                    LatencyMs = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LLMRequestLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LLMRequestLogs_AnalysisResults_AnalysisResultId",
                        column: x => x.AnalysisResultId,
                        principalTable: "AnalysisResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LLMRequestLogs_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MistakeRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnalysisResultId = table.Column<int>(type: "int", nullable: false),
                    KnowledgePointId = table.Column<int>(type: "int", nullable: true),
                    MistakeTag = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MistakeRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MistakeRecords_AnalysisResults_AnalysisResultId",
                        column: x => x.AnalysisResultId,
                        principalTable: "AnalysisResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MistakeRecords_KnowledgePoints_KnowledgePointId",
                        column: x => x.KnowledgePointId,
                        principalTable: "KnowledgePoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisResults_CreatedAt",
                table: "AnalysisResults",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisResults_ProblemId_CreatedAt",
                table: "AnalysisResults",
                columns: new[] { "ProblemId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisResults_StudentSolutionId",
                table: "AnalysisResults",
                column: "StudentSolutionId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisVisualizations_AnalysisResultId",
                table: "AnalysisVisualizations",
                column: "AnalysisResultId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_StudentNumber",
                table: "AppUsers",
                column: "StudentNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Username",
                table: "AppUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_CourseId_OrderIndex_Name",
                table: "Chapters",
                columns: new[] { "CourseId", "OrderIndex", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_SubjectId_Name",
                table: "Courses",
                columns: new[] { "SubjectId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDependencies_FromKnowledgePointId_ToKnowledgePointId",
                table: "KnowledgeDependencies",
                columns: new[] { "FromKnowledgePointId", "ToKnowledgePointId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDependencies_ToKnowledgePointId",
                table: "KnowledgeDependencies",
                column: "ToKnowledgePointId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgePoints_ChapterId",
                table: "KnowledgePoints",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgePoints_CourseId_ChapterId_Name",
                table: "KnowledgePoints",
                columns: new[] { "CourseId", "ChapterId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_LLMRequestLogs_AnalysisResultId",
                table: "LLMRequestLogs",
                column: "AnalysisResultId");

            migrationBuilder.CreateIndex(
                name: "IX_LLMRequestLogs_Provider_ModelName_CreatedAt",
                table: "LLMRequestLogs",
                columns: new[] { "Provider", "ModelName", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LLMRequestLogs_UserId",
                table: "LLMRequestLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MistakeRecords_AnalysisResultId",
                table: "MistakeRecords",
                column: "AnalysisResultId");

            migrationBuilder.CreateIndex(
                name: "IX_MistakeRecords_KnowledgePointId",
                table: "MistakeRecords",
                column: "KnowledgePointId");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_ChapterId",
                table: "Problems",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_CourseId",
                table: "Problems",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_CreatedByUserId",
                table: "Problems",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptProfiles_CourseId_Mode_Version",
                table: "PromptProfiles",
                columns: new[] { "CourseId", "Mode", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentSolutions_ProblemId",
                table: "StudentSolutions",
                column: "ProblemId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSolutions_UserId",
                table: "StudentSolutions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCourseStats_CourseId",
                table: "UserCourseStats",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCourseStats_UserId_CourseId",
                table: "UserCourseStats",
                columns: new[] { "UserId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserKnowledgeStates_KnowledgePointId",
                table: "UserKnowledgeStates",
                column: "KnowledgePointId");

            migrationBuilder.CreateIndex(
                name: "IX_UserKnowledgeStates_UserId_KnowledgePointId",
                table: "UserKnowledgeStates",
                columns: new[] { "UserId", "KnowledgePointId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisVisualizations");

            migrationBuilder.DropTable(
                name: "KnowledgeDependencies");

            migrationBuilder.DropTable(
                name: "LLMRequestLogs");

            migrationBuilder.DropTable(
                name: "MistakeRecords");

            migrationBuilder.DropTable(
                name: "PromptProfiles");

            migrationBuilder.DropTable(
                name: "UserCourseStats");

            migrationBuilder.DropTable(
                name: "UserKnowledgeStates");

            migrationBuilder.DropTable(
                name: "AnalysisResults");

            migrationBuilder.DropTable(
                name: "KnowledgePoints");

            migrationBuilder.DropTable(
                name: "StudentSolutions");

            migrationBuilder.DropTable(
                name: "Problems");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Subjects");
        }
    }
}
