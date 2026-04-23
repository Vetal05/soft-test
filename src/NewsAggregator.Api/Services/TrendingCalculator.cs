namespace NewsAggregator.Api.Services;

public static class TrendingCalculator
{
    public const int DefaultTake = 20;

    public static IReadOnlyList<(T ArticleId, int Count)> RankByWindow<T>(
        IEnumerable<(T ArticleId, DateTimeOffset CreatedAt)> bookmarks,
        DateTimeOffset now,
        TimeSpan window,
        int take = DefaultTake) where T : notnull
    {
        var from = now - window;
        return bookmarks
            .Where(b => b.CreatedAt >= from)
            .GroupBy(b => b.ArticleId)
            .Select(g => (g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Key)
            .Take(take)
            .ToList();
    }
}
