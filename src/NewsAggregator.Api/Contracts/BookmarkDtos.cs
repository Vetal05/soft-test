using System.ComponentModel.DataAnnotations;

namespace NewsAggregator.Api.Contracts;

public class CreateBookmarkRequest
{
    [Required, Range(1, int.MaxValue)]
    public int? ArticleId { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class BookmarkItem
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
