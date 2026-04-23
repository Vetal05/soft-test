using System.ComponentModel.DataAnnotations;
using NewsAggregator.Api.Models;

namespace NewsAggregator.Api.Contracts;

public class CreateArticleRequest
{
    [Required, Range(1, int.MaxValue)]
    public int? SourceId { get; set; }

    [Required, MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Summary { get; set; } = string.Empty;

    [Required, Url, MaxLength(2000)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string ImageUrl { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset? PublishedAt { get; set; }

    [Required]
    public NewsCategory? Category { get; set; }
}

public class ArticleListItem
{
    public int Id { get; set; }
    public int SourceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTimeOffset PublishedAt { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public NewsCategory Category { get; set; }
}

public class ArticleDetail : ArticleListItem
{
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
}

public class ArticleQuery
{
    public NewsCategory? Category { get; set; }
    public int? SourceId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    [Range(1, 200)]
    public int PageSize { get; set; } = 20;
}

public class TrendingItem
{
    public int Id { get; set; }
    public int SourceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public NewsCategory Category { get; set; }
    public int BookmarksInWindow { get; set; }
}
