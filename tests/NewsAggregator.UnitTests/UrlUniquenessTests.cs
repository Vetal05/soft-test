using FluentAssertions;
using NewsAggregator.Api.Services;

namespace NewsAggregator.UnitTests;

public class UrlUniquenessTests
{
    [Fact]
    public void Dup() =>
        UrlUniqueness.AnyDuplicateCaseInsensitive(new[] { "https://A/x", "https://a/x" }).Should().BeTrue();

    [Fact]
    public void None() =>
        UrlUniqueness.AnyDuplicateCaseInsensitive(new[] { "https://a/1", "https://b/1" }).Should().BeFalse();
}
