using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Api.Data;
using NewsAggregator.Api.Models;

namespace NewsAggregator.DatabaseTests;

[Collection("db")]
public class TrendingTimeWindowQueryTests
{
    private readonly PostgresContainerFixture _p;
    public TrendingTimeWindowQueryTests(PostgresContainerFixture p) => _p = p;

    [Fact]
    public async Task Window_count()
    {
        var now = DateTimeOffset.Parse("2026-04-20T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var w = TimeSpan.FromDays(7);
        var from = now - w;
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_p.ConnectionString).Options);
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync("""TRUNCATE TABLE "Sources" RESTART IDENTITY CASCADE;""");
        var s = new Source { Name = "x", Url = "https://tr", Category = NewsCategory.Sports };
        db.Sources.Add(s);
        await db.SaveChangesAsync();
        db.Articles.Add(new Article
        {
            SourceId = s.Id, Title = "a", Summary = "x", Url = "https://ta", ImageUrl = "i", PublishedAt = now, Category = NewsCategory.Sports
        });
        await db.SaveChangesAsync();
        var aid = db.Articles.AsNoTracking().First().Id;
        db.Bookmarks.Add(new Bookmark { UserId = Guid.NewGuid(), ArticleId = aid, CreatedAt = from - TimeSpan.FromHours(1) });
        db.Bookmarks.Add(new Bookmark { UserId = Guid.NewGuid(), ArticleId = aid, CreatedAt = from + TimeSpan.FromHours(1) });
        await db.SaveChangesAsync();
        (await db.Bookmarks.CountAsync(b => b.CreatedAt >= from)).Should().Be(1);
    }
}
