using Xunit;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCodeChecker.Core.DTOs;
using AutoCodeChecker.Core.Models;
using AutoCodeChecker.LocalApi;

namespace AutoCodeChecker.Tests;

public record LoginResponseDto(string Token);

public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory<AutoCodeChecker.LocalApi.Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(CustomWebApplicationFactory<AutoCodeChecker.LocalApi.Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsync(string email, string password, int role)
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var regRequest = new RegisterRequest("Test User", email, password, role);
        await _client.PostAsJsonAsync("/api/auth/register", regRequest);

        var loginRequest = new LoginRequest(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.Token);
    }

    [Fact]
    public async Task GetTasks_UnauthorizedUser_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/api/tasks");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_ValidUser_ReturnsOkStatusCode()
    {
        // Arrange
        var request = new RegisterRequest(
            FullName: "Test Student",
            Email: "student_test_1@gmail.com",
            Password: "SecurePassword123",
            Role: 0
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultMessage = await response.Content.ReadAsStringAsync();
        Assert.Contains("Реєстрація успішна", resultMessage);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorizedStatusCode()
    {
        // Arrange
        var request = new LoginRequest(
            Email: "wrong_user@gmail.com",
            Password: "WrongPassword"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTasks_AuthorizedUser_ReturnsTasksList()
    {
        // Arrange
        await AuthenticateAsync("student_get_tasks@gmail.com", "Password123", 0);

        // Act
        var response = await _client.GetAsync("/api/tasks");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tasks = await response.Content.ReadFromJsonAsync<List<object>>();
        Assert.NotNull(tasks);
    }

    [Fact]
    public async Task CreateTask_TeacherRole_CreatesTaskSuccessfully()
    {
        // Arrange
        await AuthenticateAsync("teacher_create_task@gmail.com", "Password123", 1);
        var request = new CreateTaskDto(
            Title: "Інтеграційне завдання",
            Description: "Опис завдання",
            InitialCode: "public class Solution { }",
            TestCases: new List<TestCase> { new TestCase { Inputs = new object[] { 1 }, ExpectedOutput = "1" } },
            GroupName: null,
            Deadline: DateTime.UtcNow.AddDays(7),
            MaxPoints: 100
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var createdTask = await response.Content.ReadFromJsonAsync<CodeTask>();
        Assert.NotNull(createdTask);
        Assert.Equal("Інтеграційне завдання", createdTask.Title);
    }

    [Fact]
    public async Task CreateTask_StudentRole_ReturnsForbiddenStatusCode()
    {
        // Arrange
        await AuthenticateAsync("student_hack@gmail.com", "Password123", 0);
        var request = new CreateTaskDto(
            Title: "Злам системи",
            Description: "Спроба додати завдання",
            InitialCode: "public class Solution { }",
            TestCases: new List<TestCase> { new TestCase { Inputs = new object[] { 1 }, ExpectedOutput = "1" } },
            GroupName: null,
            Deadline: null,
            MaxPoints: 100
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task JoinGroup_InvalidInviteCode_ReturnsNotFoundStatusCode()
    {
        // Arrange
        await AuthenticateAsync("student_join_group@gmail.com", "Password123", 0);
        var request = new JoinGroupRequest("WRONG6");

        // Act
        var response = await _client.PostAsJsonAsync("/api/groups/join", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTeacherResults_AuthorizedTeacher_ReturnsResultsList()
    {
        // Arrange
        await AuthenticateAsync("teacher_results_view@gmail.com", "Password123", 1);

        // Act
        var response = await _client.GetAsync("/api/teacher/results");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<object>>();
        Assert.NotNull(results);
    }
}