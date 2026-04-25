using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MathAnalysisAI.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ErrorCategory",
                table: "WrongQuestions",
                newName: "StudentAnswer");

            migrationBuilder.RenameColumn(
                name: "ContentHtml",
                table: "WrongQuestions",
                newName: "StandardSolution");

            migrationBuilder.RenameColumn(
                name: "AIDiagnosis",
                table: "WrongQuestions",
                newName: "RawLatex");

            migrationBuilder.AddColumn<string>(
                name: "CleanLatex",
                table: "WrongQuestions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ErrorAnalysis",
                table: "WrongQuestions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImprovementSuggestion",
                table: "WrongQuestions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KnowledgePoint",
                table: "WrongQuestions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OverallEvaluation",
                table: "WrongQuestions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CleanLatex",
                table: "WrongQuestions");

            migrationBuilder.DropColumn(
                name: "ErrorAnalysis",
                table: "WrongQuestions");

            migrationBuilder.DropColumn(
                name: "ImprovementSuggestion",
                table: "WrongQuestions");

            migrationBuilder.DropColumn(
                name: "KnowledgePoint",
                table: "WrongQuestions");

            migrationBuilder.DropColumn(
                name: "OverallEvaluation",
                table: "WrongQuestions");

            migrationBuilder.RenameColumn(
                name: "StudentAnswer",
                table: "WrongQuestions",
                newName: "ErrorCategory");

            migrationBuilder.RenameColumn(
                name: "StandardSolution",
                table: "WrongQuestions",
                newName: "ContentHtml");

            migrationBuilder.RenameColumn(
                name: "RawLatex",
                table: "WrongQuestions",
                newName: "AIDiagnosis");
        }
    }
}
