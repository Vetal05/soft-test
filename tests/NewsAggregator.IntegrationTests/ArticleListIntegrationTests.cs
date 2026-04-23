using System.Net.Http.Json;
using FluentAssertions;
using NewsAggregator.Api.Contracts;

namespace NewsAggregator.IntegrationTests;

[Collection("integration")]
public class ArticleListIntegrationTests
{
    private readonly HttpClient _h;
    public ArticleListIntegrationTests(PostgresWithLargeSeedFixture f) => _h = f.Factory.CreateClient();

    [Fact]
    public async Task Paged() =>
        (await _h.GetFromJsonAsync<PagedResult<ArticleListItem>>("/api/articles?page=1&pageSize=20"))!.Total
            .Should().BePositive();
}
