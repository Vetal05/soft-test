namespace NewsAggregator.Api.Models;

public class Article
{
    public int Id { get; set; }
    public int SourceId { get; set; }
    public Source? Source { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTimeOffset PublishedAt { get; set; }
    public NewsCategory Category { get; set; }
    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
}
