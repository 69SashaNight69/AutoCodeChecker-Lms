using AutoCodeChecker.Core.Models;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace AutoCodeChecker.Lambda.Assessment;

public class CodeEvaluator
{
    public async Task<List<TestResult>> RunFullTestSuite(string studentCode, List<TestCase> testCases)
    {
        var results = new List<TestResult>();

        string[] forbiddenKeywords = { "File", "Directory", "Process.Start", "Socket", "Environment.Exit", "WebClient", "HttpClient" };

        foreach (var word in forbiddenKeywords)
        {
            if (studentCode.Contains(word))
            {
                return new List<TestResult> {
                    new TestResult {
                        IsSuccess = false,
                        Input = "Security Check",
                        Actual = $"SECURITY VIOLATION: Use of forbidden keyword '{word}' is not allowed!"
                    }
                };
            }
        }

        var options = ScriptOptions.Default
            .WithReferences(
                typeof(System.Console).Assembly,
                typeof(System.Linq.Enumerable).Assembly,
                typeof(System.Collections.Generic.List<int>).Assembly
            )
            .WithImports("System", "System.Collections.Generic", "System.Linq", "System.Text");

        try
        {
            var script = CSharpScript.Create(studentCode, options);
            var compilation = script.GetCompilation();

            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);

            if (!emitResult.Success)
            {
                var diag = string.Join(", ", emitResult.Diagnostics
                    .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                    .Select(d => d.GetMessage()));
                throw new Exception($"Помилка компіляції: {diag}");
            }

            var assembly = Assembly.Load(ms.ToArray());
            var type = assembly.GetTypes().FirstOrDefault(t => t.Name == "Solution")
                       ?? throw new Exception("Клас 'Solution' не знайдено. Переконайтеся, що ваш код містить 'public class Solution'");

            var method = type.GetMethod("Execute")
                         ?? throw new Exception("Метод 'Execute' не знайдено.");

            var instance = Activator.CreateInstance(type);

            foreach (var tc in testCases)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    var methodParams = method.GetParameters();
                    var convertedInputs = new object[methodParams.Length];

                    for (int i = 0; i < methodParams.Length; i++)
                    {
                        var targetType = methodParams[i].ParameterType;
                        var rawValue = tc.Inputs[i];

                        if (rawValue == null)
                        {
                            convertedInputs[i] = null;
                            continue;
                        }

                        string rawString = rawValue.ToString();

                        try
                        {
                            convertedInputs[i] = System.Text.Json.JsonSerializer.Deserialize(rawString, targetType);
                        }
                        catch
                        {
                            try
                            {
                                convertedInputs[i] = Convert.ChangeType(rawString, targetType, System.Globalization.CultureInfo.InvariantCulture);
                            }
                            catch
                            {
                                string typeName = targetType.IsGenericType ? "Список/Масив (формат [1,2,3])" : targetType.Name;
                                throw new Exception($"Неправильний формат вводу. Параметр {i + 1} очікує тип: {typeName}");
                            }
                        }
                    }

                    var executionTask = Task.Run(() => method.Invoke(instance, convertedInputs));
                    int timeLimitMs = 2000;

                    if (await Task.WhenAny(executionTask, Task.Delay(timeLimitMs)) == executionTask)
                    {
                        stopwatch.Stop();

                        if (executionTask.IsFaulted)
                        {
                            throw executionTask.Exception.InnerException ?? executionTask.Exception;
                        }

                        var actual = executionTask.Result;

                        var jsonOptions = new System.Text.Json.JsonSerializerOptions
                        {
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        };

                        string actualStr = actual != null ? System.Text.Json.JsonSerializer.Serialize(actual, jsonOptions).Trim('"') : "null";
                        string expectedStr = tc.ExpectedOutput?.ToString()?.Trim('"') ?? "";

                        bool isSuccess;
                        if (expectedStr == "PLAYGROUND" || expectedStr == "CUSTOM_RUN")
                        {
                            isSuccess = true;
                            expectedStr = "N/A";
                        }
                        else
                        {
                            isSuccess = actualStr.Replace(" ", "") == expectedStr.Replace(" ", "");
                        }

                        results.Add(new TestResult
                        {
                            Input = string.Join(", ", tc.Inputs),
                            Expected = expectedStr,
                            Actual = actualStr,
                            IsSuccess = isSuccess,
                            ExecutionTimeMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)
                        });
                    }
                    else
                    {
                        stopwatch.Stop();
                        results.Add(new TestResult
                        {
                            Input = string.Join(", ", tc.Inputs),
                            Expected = tc.ExpectedOutput?.ToString() ?? "",
                            Actual = "Time Limit Exceeded (> 2000ms)",
                            IsSuccess = false,
                            ExecutionTimeMs = timeLimitMs
                        });
                    }
                }
                catch (TargetInvocationException ex)
                {
                    stopwatch.Stop();
                    results.Add(new TestResult
                    {
                        Input = "Runtime Error",
                        Actual = ex.InnerException?.Message ?? ex.Message,
                        IsSuccess = false,
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                    });
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    results.Add(new TestResult
                    {
                        Input = "Conversion Error",
                        Actual = ex.Message,
                        IsSuccess = false,
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                    });
                }
            }
        }
        catch (Exception ex)
        {
            results.Add(new TestResult { Input = "System Error", Actual = ex.Message, IsSuccess = false });
        }

        return results;
    }
}