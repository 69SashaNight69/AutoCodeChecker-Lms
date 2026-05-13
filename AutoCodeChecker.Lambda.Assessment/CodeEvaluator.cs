using AutoCodeChecker.Core.Models;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Encodings.Web;

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
                        Actual = $"SECURITY VIOLATION: Use of forbidden keyword '{word}' is not allowed!",
                        ExecutionTimeMs = 0
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
                       ?? throw new Exception("Клас 'Solution' не знайдено. Додайте 'public class Solution'");

            var method = type.GetMethod("Execute")
                         ?? throw new Exception("Метод 'Execute' не знайдено.");

            var instance = Activator.CreateInstance(type);

            foreach (var tc in testCases)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    var methodParams = method.GetParameters();
                    var convertedInputs = new object[methodParams.Length];

                    for (int i = 0; i < methodParams.Length; i++)
                    {
                        var targetType = methodParams[i].ParameterType;
                        var rawValue = tc.Inputs[i];
                        if (rawValue == null) { convertedInputs[i] = null; continue; }

                        string rawString = rawValue.ToString();
                        try
                        {
                            convertedInputs[i] = JsonSerializer.Deserialize(rawString, targetType);
                        }
                        catch
                        {
                            try
                            {
                                convertedInputs[i] = Convert.ChangeType(rawString, targetType, System.Globalization.CultureInfo.InvariantCulture);
                            }
                            catch
                            {
                                string typeName = targetType.IsGenericType ? "Список/Масив" : targetType.Name;
                                throw new Exception($"Неправильний тип параметра {i + 1}. Очікується: {typeName}");
                            }
                        }
                    }

                    var executionTask = Task.Run(() =>
                    {
                        try
                        {
                            return method.Invoke(instance, convertedInputs);
                        }
                        catch (TargetInvocationException ex)
                        {
                            throw ex.InnerException ?? ex;
                        }
                    });
                    int timeLimitMs = 2000;

                    var completedTask = await Task.WhenAny(executionTask, Task.Delay(timeLimitMs));
                    stopwatch.Stop();

                    if (completedTask == executionTask)
                    {
                        if (executionTask.IsFaulted)
                        {
                            var ex = executionTask.Exception?.InnerException ?? executionTask.Exception;
                            results.Add(new TestResult
                            {
                                Input = string.Join(", ", tc.Inputs),
                                Expected = tc.ExpectedOutput?.ToString() ?? "",
                                Actual = $"Runtime Error: {ex.Message}",
                                IsSuccess = false,
                                ExecutionTimeMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)
                            });
                        }
                        else
                        {
                            var actual = executionTask.Result;

                            var jsonOptions = new JsonSerializerOptions
                            {
                                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                            };

                            string actualStr = actual != null ? JsonSerializer.Serialize(actual, jsonOptions).Trim('"') : "null";
                            string expectedStr = tc.ExpectedOutput?.ToString()?.Trim('"') ?? "";

                            bool isSuccess;
                            if (expectedStr == "PLAYGROUND" || expectedStr == "CUSTOM_RUN" || expectedStr == "N/A")
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
                    }
                    else
                    {
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
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    results.Add(new TestResult
                    {
                        Input = "Error",
                        Actual = ex is TargetInvocationException tie ? tie.InnerException?.Message : ex.Message,
                        IsSuccess = false,
                        ExecutionTimeMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            results.Add(new TestResult
            {
                Input = "System",
                Actual = ex.Message,
                IsSuccess = false,
                ExecutionTimeMs = 0
            });
        }

        return results;
    }
}