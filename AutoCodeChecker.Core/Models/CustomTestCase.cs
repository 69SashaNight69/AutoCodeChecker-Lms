namespace AutoCodeChecker.Core.Models
{
    public class CustomTestCase
    {
        public List<string> Inputs { get; set; } = new();
        public string? ExpectedOutput { get; set; }
    }
}
