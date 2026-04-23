namespace NewsAggregator.Api.Models;

public class Source
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public NewsCategory Category { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
