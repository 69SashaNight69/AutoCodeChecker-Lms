namespace AutoCodeChecker.Core.DTOs;

public record RegisterRequest(string FullName, string Email, string Password, int Role);