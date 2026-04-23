using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsAggregator.Api.Contracts;
using NewsAggregator.Api.Data;
using NewsAggregator.Api.Infrastructure;
using NewsAggregator.Api.Models;

namespace NewsAggregator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SourcesController : ControllerBase
{
    private readonly AppDbContext _db;

    public SourcesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SourceListItem>>> GetAll(CancellationToken ct) =>
        Ok(await _db.Sources.AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SourceListItem
            {
                Id = s.Id, Name = s.Name, Url = s.Url, Category = s.Category, IsActive = s.IsActive
            })
            .ToListAsync(ct));

    [HttpPost]
    public async Task<ActionResult<SourceListItem>> Create([FromBody] CreateSourceRequest request, CancellationToken ct)
    {
        if (request.Category is null) return BadRequest("Category required.");
        var source = new Source
        {
            Name = request.Name,
            Url = request.Url,
            Category = request.Category.Value,
            IsActive = request.IsActive
        };
        _db.Sources.Add(source);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbExceptionMapper.IsPostgresUniqueViolation(ex))
        {
            return Conflict("Source URL must be unique.");
        }
        return Created($"/api/sources/{source.Id}", new SourceListItem
        {
            Id = source.Id, Name = source.Name, Url = source.Url, Category = source.Category, IsActive = source.IsActive
        });
    }
}
