namespace AutoCodeChecker.Core.Models
{
    public class Submission
    {
        public string StudentId { get; set; }
        public string TaskId { get; set; }
        public string SourceCode { get; set; }
        public string Language { get; set; }

        public List<string>? CustomInput { get; set; }
        public List<CustomTestCase>? CustomTests { get; set; }
    }
}