namespace AutoCodeChecker.Core.Models;

public class TestResult
{
    public string Input { get; set; }
    public string Expected { get; set; }
    public string Actual { get; set; }
    public bool IsSuccess { get; set; }

    public double ExecutionTimeMs { get; set; }
}