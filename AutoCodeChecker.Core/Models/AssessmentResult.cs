namespace AutoCodeChecker.Core.Models;

public class AssessmentResult
{
    public bool IsSuccess { get; set; }
    public int Score { get; set; }
    public List<TestResult> TestResults { get; set; } = new();
    public string AiFeedback { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}