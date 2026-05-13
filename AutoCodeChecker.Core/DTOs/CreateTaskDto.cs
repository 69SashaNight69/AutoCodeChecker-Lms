using AutoCodeChecker.Core.Models;

namespace AutoCodeChecker.Core.DTOs;

public record CreateTaskDto(
    string Title,
    string Description,
    string InitialCode,
    List<TestCase> TestCases,
    string? GroupName,
    DateTime? Deadline,
    int MaxPoints
);