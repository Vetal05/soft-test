using Bogus;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Api.Models;

namespace NewsAggregator.Api.Data;

public static class LargeDataSeeder
{
    public const int MinTotalRowCount = 10_000;
    public const int DefaultSourceCount = 150;
    public const int DefaultArticleCount = 8_200;
    public const int DefaultBookmarkCount = 1_800;

    public static async Task<SeedingStats> SeedAsync(
        AppDbContext db,
        int sourceCount = DefaultSourceCount,
        int articleCount = DefaultArticleCount,
        int bookmarkCount = DefaultBookmarkCount,
        int? seed = null,
        CancellationToken cancellationToken = default)
    {
        if (sourceCount < 1 || articleCount < 1 || bookmarkCount < 0)
            throw new ArgumentException("Invalid seed counts");

        var sSeed = seed ?? 42;
        var r = new Random(sSeed);
        var categories = (NewsCategory[])Enum.GetValues(typeof(NewsCategory));
        var fSrc = new Faker<Source>(locale: "en")
            .UseSeed(sSeed)
            .RuleFor(s => s.Name, f => f.Company.CompanyName())
            .RuleFor(s => s.Category, f => f.PickRandom(categories))
            .RuleFor(s => s.IsActive, f => f.Random.Bool(0.9f));

        var sources = new List<Source>(sourceCount);
        for (int i = 0; i < sourceCount; i++)
        {
            var s = fSrc.Generate();
            s.Name = $"{s.Name} {i:0000}";
            s.Url = $"https://source-{i:0000}-{Guid.NewGuid():n}.example.org/";
            sources.Add(s);
        }
        await db.Sources.AddRangeAsync(sources, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var sourceIds = await db.Sources.AsNoTracking().Select(s => s.Id).ToListAsync(cancellationToken);
        var fArt = new Faker<Article>(locale: "en")
            .UseSeed(sSeed + 1)
            .RuleFor(a => a.SourceId, f => f.PickRandom(sourceIds))
            .RuleFor(a => a.Title, f => f.Lorem.Sentence(4))
            .RuleFor(a => a.Summary, f => f.Lorem.Paragraph())
            .RuleFor(a => a.ImageUrl, f => f.Internet.UrlWithPath())
            .RuleFor(a => a.Category, f => f.PickRandom(categories));

        for (int i = 0; i < articleCount; i++)
        {
            var a = fArt.Generate();
            a.PublishedAt = new DateTimeOffset(
                DateTime.SpecifyKind(
                    DateTime.UtcNow.AddDays(-(i % 61)).AddSeconds(-(i % 3600)), DateTimeKind.Utc));
            a.Url = $"https://article-{i:00000000}-{Guid.NewGuid():n}.example.com/";
            db.Articles.Add(a);
        }
        await db.SaveChangesAsync(cancellationToken);

        var activeArticleIds = await (
            from ar in db.Articles.AsNoTracking()
            join s2 in db.Sources.AsNoTracking() on ar.SourceId equals s2.Id
            where s2.IsActive
            select ar.Id
        ).ToListAsync(cancellationToken);
        if (activeArticleIds.Count == 0)
            throw new InvalidOperationException("No articles from active sources for bookmarks.");

        var userIds = Enumerable.Range(0, 800).Select(_ => Guid.NewGuid()).ToList();
        var used = new HashSet<(Guid, int)>();
        var fBm = new Faker<Bookmark>(locale: "en")
            .UseSeed(sSeed + 2)
            .RuleFor(b => b.Notes, f => f.Lorem.Sentence(3));

        for (int i = 0; i < bookmarkCount; i++)
        {
            for (int guard = 0; ; guard++)
            {
                if (guard > 20_000)
                    throw new InvalidOperationException("Could not find unique bookmark pairs.");
                var userId = userIds[r.Next(userIds.Count)];
                var artId = activeArticleIds[r.Next(activeArticleIds.Count)];
                if (!used.Add((userId, artId)))
                    continue;
                var b = fBm.Generate();
                b.UserId = userId;
                b.ArticleId = artId;
                b.CreatedAt = new DateTimeOffset(
                    DateTime.SpecifyKind(
                        DateTime.UtcNow.AddDays(-(i % 15)).AddMinutes(-(i % 60)), DateTimeKind.Utc));
                db.Bookmarks.Add(b);
                break;
            }
        }
        await db.SaveChangesAsync(cancellationToken);

        int ns = await db.Sources.CountAsync(cancellationToken);
        int na = await db.Articles.CountAsync(cancellationToken);
        int nb = await db.Bookmarks.CountAsync(cancellationToken);
        int total = ns + na + nb;
        if (total < MinTotalRowCount)
            throw new InvalidOperationException($"Seeded {total} rows, need {MinTotalRowCount}.");
        return new SeedingStats(total, ns, na, nb);
    }
}

public sealed record SeedingStats(int TotalRows, int Sources, int Articles, int Bookmarks);
