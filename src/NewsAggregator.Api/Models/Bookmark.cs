namespace NewsAggregator.Api.Models;

public class Bookmark
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int ArticleId { get; set; }
    public Article? Article { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Notes { get; set; }
}
