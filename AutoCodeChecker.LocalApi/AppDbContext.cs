using Microsoft.EntityFrameworkCore;
using AutoCodeChecker.Core.Models;

namespace AutoCodeChecker.LocalApi;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<CodeTask> Tasks { get; set; }
    public DbSet<TestCase> TestCases { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<StudyGroup> Groups { get; set; }
    public DbSet<TaskResult> TaskResults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var inputsComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<object[]>(
            (c1, c2) => c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToArray());

        modelBuilder.Entity<TestCase>()
            .Property(t => t.Inputs)
            .HasConversion(
                v => string.Join("|", v),
                v => v.Split('|', StringSplitOptions.RemoveEmptyEntries).Cast<object>().ToArray())
            .Metadata.SetValueComparer(inputsComparer);

        modelBuilder.Entity<TestCase>()
            .Property(t => t.ExpectedOutput)
            .HasConversion(
                v => v.ToString(),
                v => (object)v
            );

        modelBuilder.Entity<StudyGroup>()
        .HasOne(g => g.Teacher)
        .WithMany()
        .HasForeignKey(g => g.TeacherId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StudyGroup>()
            .HasMany(g => g.Students)
            .WithMany(u => u.Groups)
            .UsingEntity(j => j.ToTable("GroupStudents"));

        modelBuilder.Entity<StudyGroup>()
            .HasMany(g => g.AssignedTasks)
            .WithMany(t => t.AssignedGroups)
            .UsingEntity(j => j.ToTable("GroupTasks"));
    }
}