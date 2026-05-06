using AutoCodeChecker.Core.Models;
using AutoCodeChecker.Lambda.Assessment;
using AutoCodeChecker.LocalApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using BCrypt.Net;
using AutoCodeChecker.Core.DTOs;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = "SuperSecretKeyForAutoCodeChecker2024!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

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

app.MapPost("/api/assess", async (HttpContext context, [FromBody] Submission request, AppDbContext db) =>
{
    var task = await db.Tasks.Include(t => t.TestCases).FirstOrDefaultAsync(t => t.Id == int.Parse(request.TaskId));
    if (task == null) return Results.NotFound("Task not found");

    List<TestCase> casesToRun = request.CustomTests != null && request.CustomTests.Any()
        ? request.CustomTests.Select(ct => new TestCase { Inputs = ct.Inputs.Cast<object>().ToArray(), ExpectedOutput = "PLAYGROUND" }).ToList()
        : task.TestCases;

    var lambdaFunction = new Function();
    var result = await lambdaFunction.EvaluateWithTests(request, casesToRun);

    if (request.CustomTests == null || !request.CustomTests.Any())
    {
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null)
        {
            var studentId = int.Parse(userIdClaim.Value);

            var existingResult = await db.TaskResults
                .FirstOrDefaultAsync(r => r.StudentId == studentId && r.TaskId == task.Id);

            if (existingResult == null)
            {
                db.TaskResults.Add(new TaskResult
                {
                    StudentId = studentId,
                    TaskId = task.Id,
                    Score = result.Score,
                    SubmittedCode = request.SourceCode,
                    AiFeedback = result.AiFeedback ?? ""
                });
            }
            else if (result.Score > existingResult.Score)
            {
                existingResult.Score = result.Score;
                existingResult.SubmittedCode = request.SourceCode;
                existingResult.AiFeedback = result.AiFeedback ?? "";
                existingResult.SubmittedAt = DateTime.UtcNow;
            }
            await db.SaveChangesAsync();
        }
    }

    if (request.CustomTests != null) result.Score = 0;
    return Results.Ok(result);
}).RequireAuthorization();

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

app.MapPost("/api/auth/register", async (RegisterRequest req, AppDbContext db) =>
{
    if (await db.Users.AnyAsync(u => u.Email == req.Email))
        return Results.BadRequest("Користувач з такою поштою вже існує");

    var user = new User
    {
        FullName = req.FullName,
        Email = req.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
        Role = (UserRole)req.Role
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok("Реєстрація успішна");
});

app.MapPost("/api/auth/login", async (LoginRequest req, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
    if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        return Results.Unauthorized();

    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(jwtKey);
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("FullName", user.FullName)
        }),
        Expires = DateTime.UtcNow.AddDays(7),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return Results.Ok(new
    {
        Token = tokenHandler.WriteToken(token),
        User = new { user.Id, user.FullName, user.Email, user.Role }
    });
});

app.MapGet("/api/teacher/results", async (AppDbContext db) =>
{
    var results = await db.TaskResults
        .Include(r => r.Student)
        .Include(r => r.Task)
        .OrderByDescending(r => r.SubmittedAt)
        .Select(r => new {
            StudentName = r.Student.FullName,
            TaskTitle = r.Task.Title,
            r.Score,
            r.SubmittedAt
        })
        .ToListAsync();
    return Results.Ok(results);
}).RequireAuthorization();

app.MapGet("/api/student/my-results", async (HttpContext context, AppDbContext db) =>
{
    var userId = int.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
    var results = await db.TaskResults
        .Include(r => r.Task)
        .Where(r => r.StudentId == userId)
        .Select(r => new { r.Task.Title, r.Score, r.SubmittedAt })
        .ToListAsync();
    return Results.Ok(results);
}).RequireAuthorization();

app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowReact");

app.Run();