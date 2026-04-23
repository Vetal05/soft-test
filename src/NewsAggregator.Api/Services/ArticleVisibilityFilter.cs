using NewsAggregator.Api.Models;

namespace NewsAggregator.Api.Services;

public static class ArticleVisibilityFilter
{
    public static bool IsVisibleInFeed(Article a) => a.Source is { IsActive: true };
}
