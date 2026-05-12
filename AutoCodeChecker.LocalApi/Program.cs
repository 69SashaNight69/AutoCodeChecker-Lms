using AutoCodeChecker.Core.Models;
using AutoCodeChecker.Lambda.Assessment;
using AutoCodeChecker.LocalApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using AutoCodeChecker.Core.DTOs;

var builder = WebApplication.CreateBuilder(args);

string GenerateInviteCode()
{
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    var random = new Random();
    return new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
}

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
        policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql("Host=localhost;Database=autocodechecker;Username=postgres;Password=123456789"));

var app = builder.Build();

app.UseCors("AllowReact");
app.UseAuthentication();
app.UseAuthorization();


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

app.MapPost("/api/groups/join", async (HttpContext context, [FromBody] JoinGroupRequest req, AppDbContext db) =>
{
    var userId = int.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier).Value);
    var group = await db.Groups.Include(g => g.Students).FirstOrDefaultAsync(g => g.InviteCode == req.Code.ToUpper());

    if (group == null) return Results.NotFound("Групу з таким кодом не знайдено");

    var student = await db.Users.FindAsync(userId);
    if (!group.Students.Any(s => s.Id == userId))
    {
        group.Students.Add(student);
        await db.SaveChangesAsync();
    }
    return Results.Ok(new { group.Id, group.Name });
}).RequireAuthorization();

app.MapGet("/api/teacher/groups", async (HttpContext context, AppDbContext db) =>
{
    var userId = int.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier).Value);
    var groups = await db.Groups.Where(g => g.TeacherId == userId)
        .Select(g => new { g.Id, g.Name, g.InviteCode, StudentsCount = g.Students.Count })
        .ToListAsync();
    return Results.Ok(groups);
}).RequireAuthorization();

app.MapGet("/api/tasks", async (HttpContext context, AppDbContext db) =>
{
    var userId = int.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
    var userRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role).Value;

    if (userRole == "Teacher")
    {
        return Results.Ok(await db.Tasks.Select(t => new {
            t.Id,
            t.Title,
            GroupName = t.AssignedGroups.Select(g => g.Name).FirstOrDefault() ?? "Загальні завдання"
        }).ToListAsync());
    }

    var userGroupIds = await db.Groups
        .Where(g => g.Students.Any(s => s.Id == userId))
        .Select(g => g.Id)
        .ToListAsync();

    var tasks = await db.Tasks
        .Where(t => !t.AssignedGroups.Any() || t.AssignedGroups.Any(g => userGroupIds.Contains(g.Id)))
        .Select(t => new {
            t.Id,
            t.Title,
            GroupName = t.AssignedGroups.Select(g => g.Name).FirstOrDefault() ?? "Загальні завдання"
        })
        .ToListAsync();

    return Results.Ok(tasks);
}).RequireAuthorization();

app.MapGet("/api/tasks/{id}", async (int id, AppDbContext db) =>
    await db.Tasks.Include(t => t.TestCases).FirstOrDefaultAsync(t => t.Id == id));

app.MapPost("/api/tasks", async (HttpContext context, [FromBody] CreateTaskDto req, AppDbContext db) =>
{
    var teacherId = int.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier).Value);

    var newTask = new CodeTask
    {
        Title = req.Title,
        Description = req.Description,
        InitialCode = req.InitialCode,
        TestCases = req.TestCases
    };

    if (!string.IsNullOrEmpty(req.GroupName))
    {
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Name == req.GroupName && g.TeacherId == teacherId);
        if (group == null)
        {
            group = new StudyGroup
            {
                Name = req.GroupName,
                TeacherId = teacherId,
                InviteCode = GenerateInviteCode(),
                Description = "Створено автоматично при додаванні завдання"
            };
            db.Groups.Add(group);
        }
        newTask.AssignedGroups.Add(group);
    }

    db.Tasks.Add(newTask);
    await db.SaveChangesAsync();
    return Results.Ok(newTask);
}).RequireAuthorization();


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
        var userId = int.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var existingResult = await db.TaskResults.FirstOrDefaultAsync(r => r.StudentId == userId && r.TaskId == task.Id);

        if (existingResult == null)
        {
            db.TaskResults.Add(new TaskResult { StudentId = userId, TaskId = task.Id, Score = result.Score, SubmittedCode = request.SourceCode, AiFeedback = result.AiFeedback ?? "" });
        }
        else if (result.Score > existingResult.Score)
        {
            existingResult.Score = result.Score;
            existingResult.SubmittedCode = request.SourceCode;
            existingResult.SubmittedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
    }

    if (request.CustomTests != null) result.Score = 0;
    return Results.Ok(result);
}).RequireAuthorization();

app.MapGet("/api/teacher/results", async (string? search, string? group, string? sortBy, AppDbContext db) =>
{
    var query = db.TaskResults
        .Include(r => r.Student).ThenInclude(s => s.Groups)
        .Include(r => r.Task).AsQueryable();

    if (!string.IsNullOrEmpty(search))
        query = query.Where(r => r.Student.FullName.Contains(search) || r.Task.Title.Contains(search));

    if (!string.IsNullOrEmpty(group))
        query = query.Where(r => r.Student.Groups.Any(g => g.Name == group));

    query = sortBy switch
    {
        "score" => query.OrderByDescending(r => r.Score),
        "student" => query.OrderBy(r => r.Student.FullName),
        _ => query.OrderByDescending(r => r.SubmittedAt)
    };

    var results = await query.ToListAsync();
    return Results.Ok(results.Select(r => new {
        StudentName = r.Student.FullName,
        TaskTitle = r.Task.Title,
        Score = r.Score,
        SubmittedAt = r.SubmittedAt,
        GroupName = r.Student.Groups.FirstOrDefault()?.Name ?? "Без групи"
    }));
}).RequireAuthorization();

app.MapGet("/api/student/my-results", async (HttpContext context, AppDbContext db) =>
{
    var userId = int.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier).Value);
    return Results.Ok(await db.TaskResults.Include(r => r.Task).Where(r => r.StudentId == userId)
        .Select(r => new { r.Task.Title, r.Score, r.SubmittedAt }).ToListAsync());
}).RequireAuthorization();

app.MapGet("/api/teacher/groups/{id}/students", async (int id, AppDbContext db) =>
{
    var group = await db.Groups
        .Include(g => g.Students)
        .FirstOrDefaultAsync(g => g.Id == id);

    if (group == null) return Results.NotFound();

    return Results.Ok(group.Students.Select(s => new { s.Id, s.FullName, s.Email }));
}).RequireAuthorization();

app.MapGet("/api/teacher/groups-details", async (HttpContext context, AppDbContext db) =>
{
    var teacherId = int.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier).Value);

    var groups = await db.Groups
        .Where(g => g.TeacherId == teacherId)
        .Select(g => new {
            g.Id,
            g.Name,
            g.InviteCode,
            Students = g.Students.Select(s => new { s.FullName, s.Email }).ToList(),
            TasksCount = g.AssignedTasks.Count
        })
        .ToListAsync();

    return Results.Ok(groups);
}).RequireAuthorization();

app.MapPut("/api/tasks/{id:int}", async (int id, HttpContext context, [FromBody] CreateTaskDto req, AppDbContext db) =>
{
    var teacherId = int.Parse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

    var task = await db.Tasks.Include(t => t.TestCases).Include(t => t.AssignedGroups).FirstOrDefaultAsync(t => t.Id == id);
    if (task == null) return Results.NotFound();

    task.Title = req.Title;
    task.Description = req.Description;
    task.InitialCode = req.InitialCode;

    db.TestCases.RemoveRange(task.TestCases);
    task.TestCases = req.TestCases ?? new List<TestCase>();

    task.AssignedGroups.Clear();
    if (!string.IsNullOrEmpty(req.GroupName))
    {
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Name == req.GroupName && g.TeacherId == teacherId);
        if (group == null)
        {
            group = new StudyGroup
            {
                Name = req.GroupName,
                TeacherId = teacherId,
                InviteCode = GenerateInviteCode(),
                Description = "Створено автоматично при редагуванні завдання"
            };
            db.Groups.Add(group);
        }
        task.AssignedGroups.Add(group);
    }

    await db.SaveChangesAsync();
    return Results.Ok(task);
}).RequireAuthorization();

app.MapDelete("/api/tasks/{id:int}", async (int id, AppDbContext db) =>
{
    var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
    if (task == null) return Results.NotFound();

    db.Tasks.Remove(task);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.Run();