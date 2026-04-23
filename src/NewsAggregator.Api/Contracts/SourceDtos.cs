using System.ComponentModel.DataAnnotations;
using NewsAggregator.Api.Models;

namespace NewsAggregator.Api.Contracts;

public class CreateSourceRequest
{
    [Required, MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required, Url, MaxLength(2000)]
    public string Url { get; set; } = string.Empty;

    [Required]
    public NewsCategory? Category { get; set; }

    public bool IsActive { get; set; } = true;
}

public class SourceListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public NewsCategory Category { get; set; }
    public bool IsActive { get; set; }
}
