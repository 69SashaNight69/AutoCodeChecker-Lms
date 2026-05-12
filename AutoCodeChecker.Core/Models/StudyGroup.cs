using System.Text.Json.Serialization;

namespace AutoCodeChecker.Core.Models;

public class StudyGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";
    public string InviteCode { get; set; } = "";

    public int TeacherId { get; set; }
    public User? Teacher { get; set; }

    [JsonIgnore]
    public List<User> Students { get; set; } = new();

    [JsonIgnore]
    public List<CodeTask> AssignedTasks { get; set; } = new();
}