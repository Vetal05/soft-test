using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Api.Contracts;
using NewsAggregator.Api.Data;
using NewsAggregator.Api.Infrastructure;
using NewsAggregator.Api.Models;

namespace NewsAggregator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookmarksController : ControllerBase
{
    private readonly AppDbContext _db;

    public BookmarksController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookmarkItem>>> List(CancellationToken ct)
    {
        if (!Guid.TryParse(Request.Headers[HeaderNames.UserId], out var userId))
            return BadRequest($"Set header {HeaderNames.UserId} to a valid GUID.");
        return Ok(await _db.Bookmarks
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .Include(b => b.Article)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BookmarkItem
            {
                Id = b.Id, ArticleId = b.ArticleId,
                Title = b.Article != null ? b.Article.Title : "", Notes = b.Notes, CreatedAt = b.CreatedAt
            })
            .ToListAsync(ct));
    }

    [HttpPost]
    public async Task<ActionResult<BookmarkItem>> Create([FromBody] CreateBookmarkRequest request, CancellationToken ct)
    {
        if (request.ArticleId is null) return BadRequest("ArticleId required.");
        if (!Guid.TryParse(Request.Headers[HeaderNames.UserId], out var userId))
            return BadRequest($"Set header {HeaderNames.UserId} to a valid GUID.");
        var row = await (from a in _db.Articles
            join s in _db.Sources on a.SourceId equals s.Id
            where a.Id == request.ArticleId
            select new { a, s.IsActive }).FirstOrDefaultAsync(ct);
        if (row is null) return BadRequest("Article not found.");
        if (!row.IsActive) return BadRequest("Source inactive.");
        var art = row.a;
        var bm = new Bookmark { UserId = userId, ArticleId = art.Id, Notes = request.Notes, CreatedAt = DateTimeOffset.UtcNow };
        _db.Bookmarks.Add(bm);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbExceptionMapper.IsPostgresUniqueViolation(ex))
        {
            return Conflict("Already bookmarked.");
        }
        return Created($"/api/bookmarks/{bm.Id}", new BookmarkItem
        {
            Id = bm.Id, ArticleId = art.Id, Title = art.Title, Notes = bm.Notes, CreatedAt = bm.CreatedAt
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!Guid.TryParse(Request.Headers[HeaderNames.UserId], out var userId))
            return BadRequest($"Set header {HeaderNames.UserId} to a valid GUID.");
        var b = await _db.Bookmarks.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (b is null) return NotFound();
        if (b.UserId != userId) return StatusCode(StatusCodes.Status403Forbidden);
        _db.Bookmarks.Remove(b);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
