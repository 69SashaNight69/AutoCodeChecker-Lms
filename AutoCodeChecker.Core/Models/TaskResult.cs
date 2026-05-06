namespace AutoCodeChecker.Core.Models;

public class TaskResult
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public User? Student { get; set; }

    public int TaskId { get; set; }
    public CodeTask? Task { get; set; }

    public int Score { get; set; }
    public string SubmittedCode { get; set; } = "";
    public string AiFeedback { get; set; } = "";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}