namespace MathAnalysisAI.Server.DTOs.Visualization
{
    public class VisualizationDto
    {
        public bool ShouldUse { get; set; }
        public string Engine { get; set; } = "geogebra";
        public string VisualizationType { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public List<string> GeoGebraCommands { get; set; } = new();
        public string? Caption { get; set; }
    }
}
