using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Api.Contracts;
using NewsAggregator.Api.Data;
using NewsAggregator.Api.Infrastructure;
using NewsAggregator.Api.Models;
using NewsAggregator.Api.Services;

namespace NewsAggregator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArticlesController : ControllerBase
{
    private readonly AppDbContext _db;
    private static readonly TimeSpan TrendingWindow = TimeSpan.FromDays(7);

    public ArticlesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PagedResult<ArticleListItem>>> List([FromQuery] ArticleQuery q, CancellationToken ct)
    {
        var baseQuery =
            from a in _db.Articles.AsNoTracking()
            join s in _db.Sources on a.SourceId equals s.Id
            where s.IsActive
            select a;
        if (q.Category is not null) baseQuery = baseQuery.Where(a => a.Category == q.Category);
        if (q.SourceId is not null) baseQuery = baseQuery.Where(a => a.SourceId == q.SourceId);
        if (q.From is not null) baseQuery = baseQuery.Where(a => a.PublishedAt >= q.From);
        if (q.To is not null) baseQuery = baseQuery.Where(a => a.PublishedAt <= q.To);
        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .OrderByDescending(a => a.PublishedAt).ThenBy(a => a.Id)
            .Skip((q.Page - 1) * q.PageSize).Take(q.PageSize)
            .Select(a => new ArticleListItem
            {
                Id = a.Id, SourceId = a.SourceId, Title = a.Title, Summary = a.Summary, Url = a.Url,
                PublishedAt = a.PublishedAt, ImageUrl = a.ImageUrl, Category = a.Category
            })
            .ToListAsync(ct);
        return Ok(new PagedResult<ArticleListItem> { Items = items, Page = q.Page, PageSize = q.PageSize, Total = total });
    }

    [HttpGet("trending")]
    public async Task<ActionResult<IReadOnlyList<TrendingItem>>> Trending(CancellationToken ct)
    {
        var from = DateTimeOffset.UtcNow - TrendingWindow;
        var ranked = await _db.Bookmarks
            .AsNoTracking()
            .Where(b => b.CreatedAt >= from)
            .Where(b => b.Article != null && b.Article.Source != null && b.Article.Source.IsActive)
            .GroupBy(b => b.ArticleId)
            .Select(g => new { g.Key, Cnt = g.Count() })
            .OrderByDescending(x => x.Cnt).ThenBy(x => x.Key)
            .Take(TrendingCalculator.DefaultTake)
            .ToListAsync(ct);
        if (ranked.Count == 0) return Ok(Array.Empty<TrendingItem>());
        var ids = ranked.Select(x => x.Key).ToList();
        var articles = await _db.Articles.AsNoTracking().Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a, ct);
        var list = new List<TrendingItem>(ranked.Count);
        foreach (var r in ranked)
        {
            if (!articles.TryGetValue(r.Key, out var a)) continue;
            list.Add(new TrendingItem
            {
                Id = a.Id, SourceId = a.SourceId, Title = a.Title, Url = a.Url, Category = a.Category, BookmarksInWindow = r.Cnt
            });
        }
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ArticleDetail>> ById(int id, CancellationToken ct)
    {
        var a = await (from art in _db.Articles.AsNoTracking()
            join s in _db.Sources on art.SourceId equals s.Id
            where art.Id == id && s.IsActive
            select art).FirstOrDefaultAsync(ct);
        if (a is null) return NotFound();
        return Ok(new ArticleDetail
        {
            Id = a.Id, SourceId = a.SourceId, Title = a.Title, Summary = a.Summary, Url = a.Url,
            PublishedAt = a.PublishedAt, ImageUrl = a.ImageUrl, Category = a.Category
        });
    }

    [HttpPost]
    public async Task<ActionResult<ArticleListItem>> Create([FromBody] CreateArticleRequest request, CancellationToken ct)
    {
        if (request is { Category: null } or { SourceId: null } or { PublishedAt: null })
            return BadRequest("SourceId, Category, PublishedAt are required.");
        var source = await _db.Sources.FindAsync(new object?[] { request.SourceId }, ct);
        if (source is null) return BadRequest("Source not found.");
        if (!source.IsActive) return BadRequest("Source is inactive.");
        var article = new Article
        {
            SourceId = source.Id, Title = request.Title, Summary = request.Summary, Url = request.Url,
            ImageUrl = request.ImageUrl, PublishedAt = request.PublishedAt!.Value, Category = request.Category.Value
        };
        _db.Articles.Add(article);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbExceptionMapper.IsPostgresUniqueViolation(ex))
        {
            return Conflict("Article URL must be unique.");
        }
        return CreatedAtAction(nameof(ById), new { id = article.Id }, new ArticleListItem
        {
            Id = article.Id, SourceId = article.SourceId, Title = article.Title, Summary = article.Summary, Url = article.Url,
            PublishedAt = article.PublishedAt, ImageUrl = article.ImageUrl, Category = article.Category
        });
    }
}
