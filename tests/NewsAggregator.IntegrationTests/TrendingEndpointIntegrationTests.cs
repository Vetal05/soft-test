using System.Net.Http.Json;
using FluentAssertions;
using NewsAggregator.Api.Contracts;

namespace NewsAggregator.IntegrationTests;

[Collection("integration")]
public class TrendingEndpointIntegrationTests
{
    private readonly HttpClient _h;
    public TrendingEndpointIntegrationTests(PostgresWithLargeSeedFixture f) => _h = f.Factory.CreateClient();

    [Fact]
    public async Task Ok()
    {
        var t = await _h.GetFromJsonAsync<List<TrendingItem>>("/api/articles/trending");
        t.Should().NotBeNull();
    }
}
