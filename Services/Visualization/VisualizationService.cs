using System.Text.Json;
using MathAnalysisAI.Server.Data;
using MathAnalysisAI.Server.DTOs.Visualization;
using MathAnalysisAI.Server.Models;

namespace MathAnalysisAI.Server.Services.Visualization
{
    public class VisualizationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IGeoGebraCommandValidator _commandValidator;

        public VisualizationService(
            ApplicationDbContext db,
            IGeoGebraCommandValidator commandValidator)
        {
            _db = db;
            _commandValidator = commandValidator;
        }

        public async Task<AnalysisVisualization?> SaveVisualizationAsync(
            int analysisResultId,
            VisualizationDto visualization,
            CancellationToken cancellationToken = default)
        {
            if (analysisResultId <= 0 || visualization == null)
            {
                return null;
            }

            // Strategy: persist an explicit "none" record when should_use=false for auditability.
            if (!visualization.ShouldUse)
            {
                var noneRecord = new AnalysisVisualization
                {
                    AnalysisResultId = analysisResultId,
                    Engine = "none",
                    VisualizationType = "none",
                    CommandsJson = JsonSerializer.Serialize(Array.Empty<string>()),
                    ViewConfigJson = "{}",
                    StepBindingJson = "{}",
                    Caption = visualization.Caption,
                    ValidationStatus = "skipped",
                    ValidationMessage = "Visualization disabled by upstream decision.",
                    CreatedAt = DateTime.UtcNow
                };

                _db.AnalysisVisualizations.Add(noneRecord);
                await _db.SaveChangesAsync(cancellationToken);
                return noneRecord;
            }

            var validation = _commandValidator.Validate(visualization.GeoGebraCommands);
            var isValid = validation.IsValid;

            var sanitizedCommands = validation.ValidCommands;
            var validationMessage = validation.Errors.Count == 0
                ? null
                : string.Join(" | ", validation.Errors.Take(8));

            var entity = new AnalysisVisualization
            {
                AnalysisResultId = analysisResultId,
                Engine = string.IsNullOrWhiteSpace(visualization.Engine) ? "geogebra" : visualization.Engine.Trim(),
                VisualizationType = visualization.VisualizationType,
                CommandsJson = JsonSerializer.Serialize(sanitizedCommands),
                ViewConfigJson = "{}",
                StepBindingJson = "{}",
                Caption = visualization.Caption,
                ValidationStatus = isValid ? "valid" : "invalid",
                ValidationMessage = validationMessage,
                CreatedAt = DateTime.UtcNow
            };

            _db.AnalysisVisualizations.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return entity;
        }
    }
}
