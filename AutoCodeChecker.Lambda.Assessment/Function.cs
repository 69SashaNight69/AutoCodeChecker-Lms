using Amazon.Lambda.Core;
using AutoCodeChecker.Core.Models;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AutoCodeChecker.Lambda.Assessment
{
    public class Function
    {
        public async Task<AssessmentResult> FunctionHandler(Submission input, ILambdaContext context)
        {
            var evaluator = new CodeEvaluator();
            var ai = new AiService();

            var testCases = new List<TestCase>
    {
        new TestCase { Inputs = new object[] { 2, 3 }, ExpectedOutput = "5" },
        new TestCase { Inputs = new object[] { 10, 20 }, ExpectedOutput = "30" },
        new TestCase { Inputs = new object[] { -1, 1 }, ExpectedOutput = "0" }
    };

            var testResults = await evaluator.RunFullTestSuite(input.SourceCode, testCases);

            int passed = testResults.Count(r => r.IsSuccess);
            string aiFeedback = await ai.GetFeedback(input.SourceCode, "Sum of two integers");

            return new AssessmentResult
            {
                IsSuccess = passed == testResults.Count,
                Score = testResults.Count > 0 ? (passed * 100) / testResults.Count : 0,
                TestResults = testResults,
                AiFeedback = aiFeedback
            };
        }

        public async Task<AssessmentResult> EvaluateWithTests(Submission input, List<TestCase> testCases)
        {
            var evaluator = new CodeEvaluator();
            var ai = new AiService();

            var testResults = await evaluator.RunFullTestSuite(input.SourceCode, testCases);

            int passed = testResults.Count(r => r.IsSuccess);
            string aiFeedback = await ai.GetFeedback(input.SourceCode, "Analysis of student code");

            return new AssessmentResult
            {
                IsSuccess = passed == testResults.Count,
                Score = testResults.Count > 0 ? (passed * 100) / testResults.Count : 0,
                TestResults = testResults,
                AiFeedback = aiFeedback
            };
        }
    }
}