using AutoCodeChecker.Core.Models;

public class CodeTask
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string InitialCode { get; set; } = "";

    public List<TestCase> TestCases { get; set; } = new();
}