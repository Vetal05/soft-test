using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NewsAggregator.Api.Contracts;

namespace NewsAggregator.IntegrationTests;

[Collection("integration")]
public class BookmarkFlowIntegrationTests
{
    private readonly HttpClient _h;
    public BookmarkFlowIntegrationTests(PostgresWithLargeSeedFixture f) => _h = f.Factory.CreateClient();

    [Fact]
    public async Task Lifecycle()
    {
        _h.DefaultRequestHeaders.Remove("X-User-Id");
        _h.DefaultRequestHeaders.Add("X-User-Id", Guid.NewGuid().ToString());
        var p = await _h.GetFromJsonAsync<PagedResult<ArticleListItem>>("/api/articles?page=1&pageSize=1");
        var aid = p!.Items[0].Id;
        var post = await _h.PostAsJsonAsync("/api/bookmarks", new CreateBookmarkRequest { ArticleId = aid, Notes = "t" });
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        var b = (await post.Content.ReadFromJsonAsync<BookmarkItem>())!;
        (await _h.GetFromJsonAsync<List<BookmarkItem>>("/api/bookmarks"))!.Should().Contain(x => x.Id == b.Id);
        Assert.Equal(HttpStatusCode.NoContent, (await _h.DeleteAsync($"/api/bookmarks/{b.Id}")).StatusCode);
    }

    [Fact]
    public async Task Double_conflict()
    {
        _h.DefaultRequestHeaders.Remove("X-User-Id");
        _h.DefaultRequestHeaders.Add("X-User-Id", Guid.NewGuid().ToString());
        var aid = (await _h.GetFromJsonAsync<PagedResult<ArticleListItem>>("/api/articles?page=1&pageSize=1"))!.Items[0].Id;
        await _h.PostAsJsonAsync("/api/bookmarks", new CreateBookmarkRequest { ArticleId = aid });
        (await _h.PostAsJsonAsync("/api/bookmarks", new CreateBookmarkRequest { ArticleId = aid })).StatusCode
            .Should().Be(HttpStatusCode.Conflict);
    }
}
