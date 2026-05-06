using System.Text.Json.Serialization;

namespace AutoCodeChecker.Core.Models;

public enum UserRole { Student, Teacher, Admin }

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";

    [JsonIgnore]
    public string PasswordHash { get; set; } = "";

    public UserRole Role { get; set; }

    [JsonIgnore]
    public List<StudyGroup> Groups { get; set; } = new();
}