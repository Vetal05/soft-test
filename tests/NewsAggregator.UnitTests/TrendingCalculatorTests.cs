using FluentAssertions;
using NewsAggregator.Api.Services;

namespace NewsAggregator.UnitTests;

public class TrendingCalculatorTests
{
    [Fact]
    public void Ranks_in_window()
    {
        var t0 = DateTimeOffset.Parse("2026-01-20T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var d = new List<(int, DateTimeOffset)>
        {
            (1, t0 - TimeSpan.FromDays(1)), (1, t0 - TimeSpan.FromDays(2)), (2, t0 - TimeSpan.FromDays(1))
        };
        var r = TrendingCalculator.RankByWindow(d, t0, TimeSpan.FromDays(7));
        r[0].Should().Be((1, 2));
    }

    [Fact]
    public void Excludes_stale()
    {
        var t0 = DateTimeOffset.Parse("2026-01-20T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        TrendingCalculator.RankByWindow(new[] { (1, t0 - TimeSpan.FromDays(8)) }, t0, TimeSpan.FromDays(7))
            .Should().BeEmpty();
    }
}
