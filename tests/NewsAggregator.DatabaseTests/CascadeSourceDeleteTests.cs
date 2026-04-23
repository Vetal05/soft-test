using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Api.Data;
using NewsAggregator.Api.Models;

namespace NewsAggregator.DatabaseTests;

[Collection("db")]
public class CascadeSourceDeleteTests
{
    private readonly PostgresContainerFixture _p;
    public CascadeSourceDeleteTests(PostgresContainerFixture p) => _p = p;

    [Fact]
    public async Task Cascade()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_p.ConnectionString).Options);
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync("""TRUNCATE TABLE "Sources" RESTART IDENTITY CASCADE;""");
        var s = new Source { Name = "c", Url = "https://cas", Category = NewsCategory.Business };
        db.Sources.Add(s);
        await db.SaveChangesAsync();
        db.Articles.Add(new Article
        {
            SourceId = s.Id, Title = "a", Summary = "x", Url = "https://ca1", ImageUrl = "i", PublishedAt = DateTimeOffset.UtcNow, Category = NewsCategory.Business
        });
        await db.SaveChangesAsync();
        db.Sources.Remove(db.Sources.AsTracking().First(x => x.Id == s.Id));
        await db.SaveChangesAsync();
        (await db.Articles.CountAsync()).Should().Be(0);
    }
}
