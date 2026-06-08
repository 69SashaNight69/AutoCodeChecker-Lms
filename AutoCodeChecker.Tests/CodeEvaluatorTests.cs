using Xunit;
using AutoCodeChecker.Lambda.Assessment;
using AutoCodeChecker.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoCodeChecker.Tests;

public class CodeEvaluatorTests
{
    private readonly CodeEvaluator _evaluator = new();

    [Fact]
    public async Task RunFullTestSuite_SimpleAddition_ReturnsSuccess()
    {
        // Arrange
        var studentCode = "public class Solution { public int Execute(int a, int b) { return a + b; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 2, 3 }, ExpectedOutput = "5" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("5", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_DivisionByZero_ReturnsRuntimeError()
    {
        // Arrange
        var studentCode = "public class Solution { public int Execute(int a, int b) { return a / b; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 10, 0 }, ExpectedOutput = "5" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Contains("Runtime Error", results[0].Actual);
        Assert.Contains("divide by zero", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_InfiniteLoop_ReturnsTimeLimitExceeded()
    {
        // Arrange
        var studentCode = "public class Solution { public int Execute(int a, int b) { while(true) {} return a + b; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 2, 3 }, ExpectedOutput = "5" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Equal("Time Limit Exceeded (> 2000ms)", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_ForbiddenKeywordFile_ReturnsSecurityViolation()
    {
        // Arrange
        var studentCode = "using System.IO; public class Solution { public int Execute(int a) { File.Delete(\"t.txt\"); return a; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 5 }, ExpectedOutput = "5" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Contains("SECURITY VIOLATION", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_ListTypeFiltering_ReturnsFilteredList()
    {
        // Arrange
        var studentCode = "using System.Collections.Generic; using System.Linq; public class Solution { public List<int> Execute(List<int> n) { return n.Where(x => x % 2 == 0).ToList(); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[1,2,3,4]" }, ExpectedOutput = "[2,4]" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("[2,4]", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_DoubleAndStringInterpolation_ReturnsFormattedString()
    {
        // Arrange
        var studentCode = "public class Solution { public string Execute(string s, double v) { return $\"{s}:{v.ToString(System.Globalization.CultureInfo.InvariantCulture)}\"; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "VAL", "10.5" }, ExpectedOutput = "VAL:10.5" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("VAL:10.5", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_SyntaxError_ReturnsCompilationError()
    {
        // Arrange
        var studentCode = "public class Solution { public int Execute(int a) { return a + ; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 5 }, ExpectedOutput = "5" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Contains("Помилка компіляції", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_MissingSolutionClass_ReturnsErrorResult()
    {
        // Arrange
        var studentCode = "public class WrongName { public int Execute(int a) { return a; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 5 }, ExpectedOutput = "5" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Contains("Solution", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_MissingExecuteMethod_ReturnsErrorResult()
    {
        // Arrange
        var studentCode = "public class Solution { public int WrongMethod(int a) { return a; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 5 }, ExpectedOutput = "5" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Contains("Execute", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_BooleanNegation_ReturnsInvertedBoolean()
    {
        // Arrange
        var studentCode = "public class Solution { public bool Execute(bool b) { return !b; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "true" }, ExpectedOutput = "false" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("false", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_InvalidInputType_ReturnsConversionError()
    {
        // Arrange
        var studentCode = "public class Solution { public int Execute(int a) { return a; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "not-a-number" }, ExpectedOutput = "5" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Contains("Error", results[0].Input);
    }

    [Fact]
    public async Task RunFullTestSuite_WrongParameterCount_ReturnsCompilationError()
    {
        // Arrange
        var studentCode = "public class Solution { public int Execute(int a, int b) { return a + b; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 5 }, ExpectedOutput = "15" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Equal("Error", results[0].Input);
    }

    [Fact]
    public async Task RunFullTestSuite_FloatDivision_ReturnsFloatValue()
    {
        // Arrange
        var studentCode = "public class Solution { public float Execute(float a, float b) { return a / b; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "5.0", "2.0" }, ExpectedOutput = "2.5" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("2.5", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_LongIntegerMath_ReturnsLargeNumber()
    {
        // Arrange
        var studentCode = "public class Solution { public long Execute(long a, long b) { return a + b; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "3000000000", "2000000000" }, ExpectedOutput = "5000000000" } };

        // Act
        var results = (await _evaluator.RunFullTestSuite(studentCode, testCases)).ToArray();

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("5000000000", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_StringArrayLength_ReturnsCorrectCount()
    {
        // Arrange
        var studentCode = "public class Solution { public int Execute(string[] arr) { return arr.Length; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[\"a\",\"b\",\"c\"]" }, ExpectedOutput = "3" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("3", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_NullStringArgument_HandlesNullGracefully()
    {
        // Arrange
        var studentCode = "public class Solution { public string Execute(string s) { return s ?? \"empty\"; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { null }, ExpectedOutput = "empty" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("empty", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_EmptyArray_ReturnsZeroSum()
    {
        // Arrange
        var studentCode = "using System.Linq; public class Solution { public int Execute(int[] arr) { return arr.Sum(); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[]" }, ExpectedOutput = "0" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("0", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_ArraySorting_ReturnsSortedArray()
    {
        // Arrange
        var studentCode = "using System; public class Solution { public int[] Execute(int[] arr) { Array.Sort(arr); return arr; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[3,1,2]" }, ExpectedOutput = "[1,2,3]" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("[1,2,3]", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_CustomPlaygroundMark_ReturnsSuccessWithNAPlaceholder()
    {
        // Arrange
        var studentCode = "public class Solution { public int Execute(int a) { return a; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 5 }, ExpectedOutput = "PLAYGROUND" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("N/A", results[0].Expected);
    }

    [Fact]
    public async Task RunFullTestSuite_ForbiddenDirectoryKeyword_ReturnsSecurityViolation()
    {
        // Arrange
        var studentCode = "using System.IO; public class Solution { public int Execute(int a) { Directory.CreateDirectory(\"d\"); return a; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 1 }, ExpectedOutput = "1" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Contains("SECURITY VIOLATION", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_ForbiddenProcessKeyword_ReturnsSecurityViolation()
    {
        // Arrange
        var studentCode = "using System.Diagnostics; public class Solution { public int Execute(int a) { Process.Start(\"cmd.exe\"); return a; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 1 }, ExpectedOutput = "1" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Contains("SECURITY VIOLATION", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_ForbiddenSocketKeyword_ReturnsSecurityViolation()
    {
        // Arrange
        var studentCode = "using System.Net.Sockets; public class Solution { public int Execute(int a) { var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); return a; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 1 }, ExpectedOutput = "1" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Contains("SECURITY VIOLATION", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_ForbiddenExitKeyword_ReturnsSecurityViolation()
    {
        // Arrange
        var studentCode = "using System; public class Solution { public int Execute(int a) { Environment.Exit(0); return a; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 1 }, ExpectedOutput = "1" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Contains("SECURITY VIOLATION", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_ForbiddenHttpClientKeyword_ReturnsSecurityViolation()
    {
        // Arrange
        var studentCode = "using System.Net.Http; public class Solution { public int Execute(int a) { var c = new HttpClient(); return a; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 1 }, ExpectedOutput = "1" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Contains("SECURITY VIOLATION", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_StringListConcatenation_ReturnsCombinedString()
    {
        // Arrange
        var studentCode = "using System.Collections.Generic; public class Solution { public string Execute(List<string> l) { return string.Join(\"-\", l); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[\"a\",\"b\"]" }, ExpectedOutput = "a-b" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("a-b", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_CharCountInString_ReturnsCorrectOccurrences()
    {
        // Arrange
        var studentCode = "using System.Linq; public class Solution { public int Execute(string s, string c) { return s.Count(x => x == c[0]); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "apple", "p" }, ExpectedOutput = "2" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("2", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_StringContainment_ReturnsTrueIfContains()
    {
        // Arrange
        var studentCode = "public class Solution { public bool Execute(string s, string s2) { return s.Contains(s2); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "hello world", "world" }, ExpectedOutput = "true" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("true", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_MaxElementInList_ReturnsMaxInteger()
    {
        // Arrange
        var studentCode = "using System.Collections.Generic; using System.Linq; public class Solution { public int Execute(List<int> n) { return n.Max(); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[1,5,3]" }, ExpectedOutput = "5" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("5", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_ListElementReversal_ReturnsReversedSequence()
    {
        // Arrange
        var studentCode = "using System.Collections.Generic; using System.Linq; public class Solution { public List<int> Execute(List<int> n) { n.Reverse(); return n; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[1,2,3]" }, ExpectedOutput = "[3,2,1]" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("[3,2,1]", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_NumberListSum_ReturnsTotalValue()
    {
        // Arrange
        var studentCode = "using System.Collections.Generic; using System.Linq; public class Solution { public int Execute(List<int> n) { return n.Sum(); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[10,20,30]" }, ExpectedOutput = "60" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("60", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_EmptyStringInput_ReturnsTrue()
    {
        // Arrange
        var studentCode = "public class Solution { public bool Execute(string s) { return string.IsNullOrEmpty(s); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "" }, ExpectedOutput = "true" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("true", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_DictionaryLookup_ReturnsMappedValue()
    {
        // Arrange
        var studentCode = "using System.Collections.Generic; public class Solution { public int Execute(string key) { var d = new Dictionary<string, int> { {\"a\", 1} }; return d[key]; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "a" }, ExpectedOutput = "1" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("1", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_NegativeIntegers_ReturnsNegativeSum()
    {
        // Arrange
        var studentCode = "public class Solution { public int Execute(int a, int b) { return a + b; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { -5, -10 }, ExpectedOutput = "-15" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("-15", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_LargeDoublePrecision_ReturnsAccurateDouble()
    {
        // Arrange
        var studentCode = "public class Solution { public double Execute(double a, double b) { return a * b; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "1.00005", "2.0" }, ExpectedOutput = "2.0001" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("2.0001", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_ListLengthCount_ReturnsCollectionSize()
    {
        // Arrange
        var studentCode = "using System.Collections.Generic; public class Solution { public int Execute(List<int> l) { return l.Count; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[1,2,3,4,5]" }, ExpectedOutput = "5" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("5", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_StringTrimming_ReturnsCleanedString()
    {
        // Arrange
        var studentCode = "public class Solution { public string Execute(string s) { return s.Trim(); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "  hello  " }, ExpectedOutput = "hello" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("hello", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_EmptyListMax_ReturnsRuntimeError()
    {
        // Arrange
        var studentCode = "using System.Collections.Generic; using System.Linq; public class Solution { public int Execute(List<int> n) { return n.Max(); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[]" }, ExpectedOutput = "0" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Contains("Runtime Error", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_WrongParameterCount_ReturnsConversionError()
    {
        // Arrange
        var studentCode = "public class Solution { public int Execute(int a, int b) { return a + b; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 5 }, ExpectedOutput = "15" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Equal("Error", results[0].Input);
    }

    [Fact]
    public async Task RunFullTestSuite_IntegerOverflow_ReturnsWrappedValue()
    {
        // Arrange
        var studentCode = "public class Solution { public int Execute(int a) { return a + 1; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "2147483647" }, ExpectedOutput = "-2147483648" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("-2147483648", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_StringSplitting_ReturnsSecondElement()
    {
        // Arrange
        var studentCode = "public class Solution { public string Execute(string s) { return s.Split(',')[1]; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "a,b,c" }, ExpectedOutput = "b" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("b", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_ParseHexadecimal_ReturnsDecimal()
    {
        // Arrange
        var studentCode = "using System; public class Solution { public int Execute(string hex) { return Convert.ToInt32(hex, 16); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "1A" }, ExpectedOutput = "26" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("26", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_CalculateFactorial_ReturnsCorrectValue()
    {
        // Arrange
        var studentCode = "public class Solution { public int Execute(int n) { int r = 1; for(int i=1; i<=n; i++) r *= i; return r; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 5 }, ExpectedOutput = "120" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("120", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_FibonacciSequence_ReturnsNthNumber()
    {
        // Arrange
        var studentCode = "public class Solution { public int Execute(int n) { if(n<=1) return n; int a=0, b=1; for(int i=2; i<=n; i++) { int c=a+b; a=b; b=c; } return b; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 6 }, ExpectedOutput = "8" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("8", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_DecimalPrecisionMultiplication_ReturnsCorrectProduct()
    {
        // Arrange
        var studentCode = "public class Solution { public decimal Execute(decimal a, decimal b) { return a * b; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "1.25", "4" }, ExpectedOutput = "5.00" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("5.00", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_CheckPrimeNumber_ReturnsTrue()
    {
        // Arrange
        var studentCode = "public class Solution { public bool Execute(int n) { if(n<=1) return false; for(int i=2; i*i<=n; i++) if(n%i==0) return false; return true; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 17 }, ExpectedOutput = "true" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("true", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_StringAllDigits_ReturnsTrueIfOnlyDigits()
    {
        // Arrange
        var studentCode = "using System.Linq; public class Solution { public bool Execute(string s) { return s.All(char.IsDigit); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "12345" }, ExpectedOutput = "true" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("true", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_StringReverseInPlace_ReturnsReversedString()
    {
        // Arrange
        var studentCode = "using System; public class Solution { public string Execute(string s) { char[] arr = s.ToCharArray(); Array.Reverse(arr); return new string(arr); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "hello" }, ExpectedOutput = "olleh" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("olleh", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_CountWordsInText_ReturnsCorrectWordCount()
    {
        // Arrange
        var studentCode = "using System; public class Solution { public int Execute(string s) { return s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "hello world from csharp" }, ExpectedOutput = "4" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("4", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_ReplaceSubstringWithPattern_ReturnsModifiedString()
    {
        // Arrange
        var studentCode = "public class Solution { public string Execute(string s, string pattern, string rep) { return s.Replace(pattern, rep); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "apple apple", "apple", "orange" }, ExpectedOutput = "orange orange" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("orange orange", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_Base64Encoding_ReturnsOriginalString()
    {
        // Arrange
        var studentCode = "using System; using System.Text; public class Solution { public string Execute(string s) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(s)); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "hello" }, ExpectedOutput = "aGVsbG8=" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("aGVsbG8=", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_DateTimeDifference_ReturnsDaysDifference()
    {
        // Arrange
        var studentCode = "using System; public class Solution { public double Execute(string s1, string s2) { return (DateTime.Parse(s2) - DateTime.Parse(s1)).TotalDays; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "2026-05-10", "2026-05-15" }, ExpectedOutput = "5" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("5", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_CheckLeapYear_ReturnsTrueIfLeap()
    {
        // Arrange
        var studentCode = "using System; public class Solution { public bool Execute(int y) { return DateTime.IsLeapYear(y); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 2024 }, ExpectedOutput = "true" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("true", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_FormatDateTimeToString_ReturnsCustomFormattedString()
    {
        // Arrange
        var studentCode = "using System; public class Solution { public string Execute(string d) { return DateTime.Parse(d).ToString(\"dd-MM-yyyy\"); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "2026-05-13" }, ExpectedOutput = "13-05-2026" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("13-05-2026", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_DateTimeAddition_ReturnsUpdatedDateTime()
    {
        // Arrange
        var studentCode = "using System; public class Solution { public string Execute(string d, int days) { return DateTime.Parse(d).AddDays(days).ToString(\"yyyy-MM-dd\"); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "2026-05-13", 5 }, ExpectedOutput = "2026-05-18" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("2026-05-18", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_DayOfWeekExtraction_ReturnsNameOfDay()
    {
        // Arrange
        var studentCode = "using System; public class Solution { public string Execute(string d) { return DateTime.Parse(d).DayOfWeek.ToString(); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "2026-05-10" }, ExpectedOutput = "Sunday" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("Sunday", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_ArrayMinMaxDifference_ReturnsDifference()
    {
        // Arrange
        var studentCode = "using System.Linq; public class Solution { public int Execute(int[] arr) { return arr.Max() - arr.Min(); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[10,2,8,20]" }, ExpectedOutput = "18" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("18", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_RemoveDuplicatesFromList_ReturnsUniqueElements()
    {
        // Arrange
        var studentCode = "using System.Collections.Generic; using System.Linq; public class Solution { public List<int> Execute(List<int> l) { return l.Distinct().ToList(); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[1,1,2,2,3]" }, ExpectedOutput = "[1,2,3]" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("[1,2,3]", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_ListIntersection_ReturnsCommonElements()
    {
        // Arrange
        var studentCode = "using System.Collections.Generic; using System.Linq; public class Solution { public List<int> Execute(List<int> l1, List<int> l2) { return l1.Intersect(l2).ToList(); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[1,2,3]", "[2,3,4]" }, ExpectedOutput = "[2,3]" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("[2,3]", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_FlattenNestedList_ReturnsFlatArray()
    {
        // Arrange
        var studentCode = "using System.Collections.Generic; using System.Linq; public class Solution { public List<int> Execute(List<List<int>> nested) { return nested.SelectMany(x => x).ToList(); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[[1,2],[3,4]]" }, ExpectedOutput = "[1,2,3,4]" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("[1,2,3,4]", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_FindIndexInArray_ReturnsExpectedIndex()
    {
        // Arrange
        var studentCode = "using System; public class Solution { public int Execute(string[] arr, string target) { return Array.IndexOf(arr, target); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[\"a\",\"b\",\"c\"]", "b" }, ExpectedOutput = "1" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("1", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_EnumParsing_ReturnsEnumValue()
    {
        // Arrange
        var studentCode = "using System; public class Solution { public string Execute(string s) { return Enum.Parse(typeof(DayOfWeek), s).ToString(); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "Monday" }, ExpectedOutput = "Monday" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("Monday", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_StringToGuid_ReturnsValidGuid()
    {
        // Arrange
        var studentCode = "using System; public class Solution { public Guid Execute(string s) { return Guid.Parse(s); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "d3b07384-d113-44b6-a4bc-d11344b6a4bc" }, ExpectedOutput = "d3b07384-d113-44b6-a4bc-d11344b6a4bc" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("d3b07384-d113-44b6-a4bc-d11344b6a4bc", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_TimeSpanParsing_ReturnsTotalSeconds()
    {
        // Arrange
        var studentCode = "using System; public class Solution { public double Execute(string s) { return TimeSpan.Parse(s).TotalSeconds; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "00:01:30" }, ExpectedOutput = "90" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("90", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_NullableTypesWithNull_ReturnsFallback()
    {
        // Arrange
        var studentCode = "public class Solution { public int Execute(int? a) { return a ?? -1; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { null }, ExpectedOutput = "-1" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("-1", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_QueryStringParsing_ReturnsValue()
    {
        // Arrange
        var studentCode = "using System.Linq; public class Solution { public string Execute(string query) { return query.Split('&').Select(p => p.Split('=')).ToDictionary(a => a[0], a => a[1])[\"key\"]; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "key=value" }, ExpectedOutput = "value" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("value", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_ForbiddenWebClientKeyword_ReturnsSecurityViolation()
    {
        // Arrange
        var studentCode = "using System.Net; public class Solution { public int Execute(int a) { var w = new WebClient(); return a; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 1 }, ExpectedOutput = "1" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Contains("SECURITY VIOLATION", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_ExecutionTimeoutWithLongRunningTask_ReturnsTimeLimitExceeded()
    {
        // Arrange
        var studentCode = "using System.Threading; public class Solution { public int Execute(int a) { Thread.Sleep(3000); return a; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 1 }, ExpectedOutput = "1" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Equal("Time Limit Exceeded (> 2000ms)", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_UnwrapNestedTargetInvocationException_ReturnsRealMessage()
    {
        // Arrange
        var studentCode = "using System; public class Solution { public int Execute(int a) { throw new InvalidOperationException(\"Inner exception message\"); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 1 }, ExpectedOutput = "1" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Contains("Inner exception message", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_NullReferenceExceptionInStudentCode_ReturnsRuntimeError()
    {
        // Arrange
        var studentCode = "public class Solution { public int Execute(string s) { return s.Length; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { null }, ExpectedOutput = "0" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsSuccess);
        Assert.Contains("Runtime Error", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_DictionarySerialization_ReturnsJsonString()
    {
        // Arrange
        var studentCode = "using System.Collections.Generic; public class Solution { public Dictionary<string, int> Execute() { return new Dictionary<string, int> { {\"x\", 10} }; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { }, ExpectedOutput = "{\"x\":10}" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("{\"x\":10}", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_BitwiseOperations_ReturnsExpectedMask()
    {
        // Arrange
        var studentCode = "public class Solution { public int Execute(int a, int b) { return a & b; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { 12, 10 }, ExpectedOutput = "8" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("8", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_FindDuplicatesInList_ReturnsDuplicatesOnly()
    {
        // Arrange
        var studentCode = "using System.Collections.Generic; using System.Linq; public class Solution { public List<int> Execute(List<int> l) { return l.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList(); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[1,2,2,3,3,4]" }, ExpectedOutput = "[2,3]" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("[2,3]", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_CheckPalindrome_ReturnsTrueIfPalindrome()
    {
        // Arrange
        var studentCode = "using System.Linq; public class Solution { public bool Execute(string s) { return s.SequenceEqual(s.Reverse()); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "radar" }, ExpectedOutput = "true" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("true", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_CountCharacterCaseSensitively_ReturnsOccurrences()
    {
        // Arrange
        var studentCode = "using System.Linq; public class Solution { public int Execute(string s, string c) { return s.Count(x => x == c[0]); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "Apple", "A" }, ExpectedOutput = "1" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("1", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_SumFloatPrecisionCheck_ReturnsExpectedSum()
    {
        // Arrange
        var studentCode = "public class Solution { public float Execute(float a, float b) { return a + b; } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "1.0001", "2.0002" }, ExpectedOutput = "3.0003" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("3.0003", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_ListIntersectionEmpty_ReturnsEmptyArray()
    {
        // Arrange
        var studentCode = "using System.Collections.Generic; using System.Linq; public class Solution { public List<int> Execute(List<int> l1, List<int> l2) { return l1.Intersect(l2).ToList(); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[1,2,3]", "[4,5,6]" }, ExpectedOutput = "[]" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("[]", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_StringToUpperConversion_ReturnsCapitalizedString()
    {
        // Arrange
        var studentCode = "public class Solution { public string Execute(string s) { return s.ToUpper(); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "hello" }, ExpectedOutput = "HELLO" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("HELLO", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_ListAnyMatch_ReturnsTrueIfMatchExists()
    {
        // Arrange
        var studentCode = "using System.Collections.Generic; using System.Linq; public class Solution { public bool Execute(List<int> l, int target) { return l.Any(x => x == target); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[1,2,3]", 2 }, ExpectedOutput = "true" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("true", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_StringArrayJoinWithCommas_ReturnsCommaSeparatedString()
    {
        // Arrange
        var studentCode = "using System; public class Solution { public string Execute(string[] arr) { return string.Join(\",\", arr); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "[\"x\",\"y\",\"z\"]" }, ExpectedOutput = "x,y,z" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("x,y,z", results[0].Actual);
    }

    [Fact]
    public async Task RunFullTestSuite_DictionaryKeyCheck_ReturnsTrueIfKeyExists()
    {
        // Arrange
        var studentCode = "using System.Collections.Generic; public class Solution { public bool Execute(string key) { var d = new Dictionary<string, int> { {\"test\", 1} }; return d.ContainsKey(key); } }";
        var testCases = new List<TestCase> { new TestCase { Inputs = new object[] { "test" }, ExpectedOutput = "true" } };

        // Act
        var results = await _evaluator.RunFullTestSuite(studentCode, testCases);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("true", results[0].Actual);
    }
}