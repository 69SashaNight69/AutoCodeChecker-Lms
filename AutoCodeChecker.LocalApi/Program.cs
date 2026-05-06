using AutoCodeChecker.Core.Models;
using AutoCodeChecker.Lambda.Assessment;
using AutoCodeChecker.LocalApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql("Host=localhost;Database=autocodechecker;Username=postgres;Password=123456789"));

var app = builder.Build();

app.UseCors("AllowReact");

app.MapPost("/api/assess", async ([FromBody] Submission request, AppDbContext db) =>
{
    var task = await db.Tasks.Include(t => t.TestCases)
                             .FirstOrDefaultAsync(t => t.Id == int.Parse(request.TaskId));
    if (task == null) return Results.NotFound("Task not found");

    List<TestCase> casesToRun = new();

    if (request.CustomTests != null && request.CustomTests.Any())
    {
        casesToRun = request.CustomTests.Select(ct => new TestCase
        {
            Inputs = ct.Inputs.Cast<object>().ToArray(),
            ExpectedOutput = "PLAYGROUND"
        }).ToList();
    }
    else
    {
        casesToRun = task.TestCases;
    }

    var lambdaFunction = new Function();
    var result = await lambdaFunction.EvaluateWithTests(request, casesToRun);

    if (request.CustomTests != null) result.Score = 0;

    return Results.Ok(result);
});

app.MapGet("/api/tasks", async (AppDbContext db) =>
    await db.Tasks.Select(t => new { t.Id, t.Title }).ToListAsync());

app.MapGet("/api/tasks/{id}", async (int id, AppDbContext db) =>
    await db.Tasks.Include(t => t.TestCases).FirstOrDefaultAsync(t => t.Id == id));

app.MapPost("/api/tasks", async ([FromBody] CodeTask newTask, AppDbContext db) =>
{
    db.Tasks.Add(newTask);
    await db.SaveChangesAsync();
    return Results.Ok(newTask);
});

app.MapPut("/api/tasks/{id:int}", async (int id, [FromBody] CodeTask updatedTask, AppDbContext db) =>
{
    var task = await db.Tasks.Include(t => t.TestCases).FirstOrDefaultAsync(t => t.Id == id);
    if (task == null) return Results.NotFound();

    task.Title = updatedTask.Title;
    task.Description = updatedTask.Description;
    task.InitialCode = updatedTask.InitialCode;

    db.TestCases.RemoveRange(task.TestCases);
    task.TestCases = updatedTask.TestCases;

    await db.SaveChangesAsync();
    return Results.Ok(task);
});

app.MapDelete("/api/tasks/{id:int}", async (int id, AppDbContext db) =>
{
    var task = await db.Tasks.FindAsync(id);
    if (task == null) return Results.NotFound();

    db.Tasks.Remove(task);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();