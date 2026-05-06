namespace AutoCodeChecker.Core.Models;

public class TestCase
{
    public int Id { get; set; }
    public object[] Inputs { get; set; } = Array.Empty<object>();
    public object ExpectedOutput { get; set; } = "";

    public int CodeTaskId { get; set; }
}