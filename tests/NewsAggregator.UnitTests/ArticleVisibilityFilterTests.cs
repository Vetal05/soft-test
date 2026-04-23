using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using NewsAggregator.Api.Models;
using NewsAggregator.Api.Services;

namespace NewsAggregator.UnitTests;

public class ArticleVisibilityFilterTests
{
    [Theory, AutoData]
    public void Inactive_hidden(NewsCategory c)
    {
        var s = new Source { IsActive = false, Category = c, Name = "n", Url = "https://s" };
        var a = new Article
        {
            Id = 1, Source = s, SourceId = 1, Category = c, Title = "t", Summary = "x", Url = "https://a"
        };
        ArticleVisibilityFilter.IsVisibleInFeed(a).Should().BeFalse();
    }

    [Fact]
    public void Autofixture_name_omit_articles()
    {
        var f = new Fixture();
        f.Customize<Source>(c => c
            .Without(s => s.Id)
            .Without(s => s.Articles)
            .With(s => s.Url, "https://u.local/1")
            .With(s => s.Category, NewsCategory.Tech)
            .With(s => s.IsActive, true));
        f.Create<Source>().Name.Should().NotBeNullOrEmpty();
    }
}
