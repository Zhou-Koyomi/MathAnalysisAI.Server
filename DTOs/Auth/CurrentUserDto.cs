namespace MathAnalysisAI.Server.DTOs.Auth;

public class CurrentUserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? RealName { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? SchoolName { get; set; }
    public string? DepartmentName { get; set; }
    public string? ClassName { get; set; }
}
