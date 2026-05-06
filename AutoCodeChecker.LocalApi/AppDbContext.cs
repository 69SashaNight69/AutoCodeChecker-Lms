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
    }
}