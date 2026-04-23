using Microsoft.EntityFrameworkCore;
using NewsAggregator.Api.Data;
using NewsAggregator.Api.Models;

namespace NewsAggregator.DatabaseTests;

[Collection("db")]
public class UrlUniquenessConstraintTests
{
    private readonly PostgresContainerFixture _p;
    public UrlUniquenessConstraintTests(PostgresContainerFixture p) => _p = p;

    [Fact]
    public async Task Source_url()
    {
        await using var db = Ctx();
        await R(db);
        db.Sources.Add(new Source { Name = "a", Url = "https://dup-s", Category = NewsCategory.Tech });
        await db.SaveChangesAsync();
        db.Sources.Add(new Source { Name = "b", Url = "https://dup-s", Category = NewsCategory.Sports });
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal("23505", (ex.InnerException as Npgsql.NpgsqlException)?.SqlState);
    }

    [Fact]
    public async Task Article_url()
    {
        await using var db = Ctx();
        await R(db);
        var s = new Source { Name = "s", Url = "https://s-u", Category = NewsCategory.Tech };
        db.Sources.Add(s);
        await db.SaveChangesAsync();
        db.Articles.Add(new Article
        {
            SourceId = s.Id, Title = "a", Summary = "x", Url = "https://d-a", ImageUrl = "i", PublishedAt = DateTimeOffset.UtcNow, Category = NewsCategory.Tech
        });
        await db.SaveChangesAsync();
        db.Articles.Add(new Article
        {
            SourceId = s.Id, Title = "b", Summary = "x", Url = "https://d-a", ImageUrl = "i", PublishedAt = DateTimeOffset.UtcNow, Category = NewsCategory.Tech
        });
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal("23505", (ex.InnerException as Npgsql.NpgsqlException)?.SqlState);
    }

    [Fact]
    public async Task Bookmark_pair()
    {
        await using var db = Ctx();
        await R(db);
        var s = new Source { Name = "s2", Url = "https://s2", Category = NewsCategory.Tech };
        db.Sources.Add(s);
        await db.SaveChangesAsync();
        db.Articles.Add(new Article
        {
            SourceId = s.Id, Title = "t", Summary = "x", Url = "https://art1", ImageUrl = "i", PublishedAt = DateTimeOffset.UtcNow, Category = NewsCategory.Tech
        });
        await db.SaveChangesAsync();
        var aid = db.Articles.AsNoTracking().First().Id;
        var u = Guid.NewGuid();
        db.Bookmarks.Add(new Bookmark { UserId = u, ArticleId = aid, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        db.Bookmarks.Add(new Bookmark { UserId = u, ArticleId = aid, CreatedAt = DateTimeOffset.UtcNow });
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal("23505", (ex.InnerException as Npgsql.NpgsqlException)?.SqlState);
    }

    private AppDbContext Ctx() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_p.ConnectionString).Options);
    private static async Task R(AppDbContext db)
    {
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync("""TRUNCATE TABLE "Sources" RESTART IDENTITY CASCADE;""");
    }
}
