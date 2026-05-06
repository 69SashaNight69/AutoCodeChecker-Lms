using AutoCodeChecker.Core.Models;
using System.Text.Json.Serialization;

public class CodeTask
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string InitialCode { get; set; } = "";

    public List<TestCase> TestCases { get; set; } = new();

    [JsonIgnore]
    public List<StudyGroup> AssignedGroups { get; set; } = new();

    [JsonIgnore]
    public List<TaskResult> Results { get; set; } = new();
}