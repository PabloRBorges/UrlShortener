using UrlShortener.Api.Data;
using UrlShortener.Api.Domain;
using UrlShortener.Api.Services;
using UrlShortener.Tests.Fakes;

namespace UrlShortener.Tests.Unit;

public class LinkAnalyticsServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryUrlRepository _repo = new();
    private readonly FakeClock _clock = new(Now);
    private readonly LinkAnalyticsService _sut;

    public LinkAnalyticsServiceTests() => _sut = new(_repo, _clock);

    private void AddLink(string code, DateTimeOffset createdAt) =>
        _repo.TryAdd(new ShortLink(code, $"https://{code}.com", createdAt, null));

    private void AddClick(string code, DateTimeOffset at, string? referrer = null) =>
        _repo.AddClick(new Click(code, referrer, "ua", "ip", at));

    [Fact]
    public void ListLinks_returns_newest_first_with_click_count()
    {
        AddLink("old0000", Now.AddDays(-2));
        AddLink("new0000", Now.AddDays(-1));
        AddClick("old0000", Now);
        AddClick("old0000", Now);

        var list = _sut.ListLinks();

        Assert.Equal(["new0000", "old0000"], list.Select(l => l.Link.ShortCode));
        Assert.Equal(0, list[0].ClickCount);
        Assert.Equal(2, list[1].ClickCount);
    }

    [Fact]
    public void GetStats_returns_null_for_unknown_code()
    {
        Assert.Null(_sut.GetStats("nope000"));
    }

    [Fact]
    public void GetStats_counts_total_clicks()
    {
        AddLink("abc0000", Now);
        AddClick("abc0000", Now);
        AddClick("abc0000", Now.AddDays(-40));

        Assert.Equal(2, _sut.GetStats("abc0000")!.TotalClicks);
    }

    [Fact]
    public void GetStats_groups_clicks_by_day_for_last_30_days_only()
    {
        AddLink("abc0000", Now);
        AddClick("abc0000", Now);
        AddClick("abc0000", Now.AddHours(-1));
        AddClick("abc0000", Now.AddDays(-29));
        AddClick("abc0000", Now.AddDays(-31));

        var byDay = _sut.GetStats("abc0000")!.ClicksByDay;

        Assert.Equal(30, byDay.Count);
        Assert.Equal(new DateOnly(2026, 10, 11), byDay[0].Day);
        Assert.Equal(1, byDay[0].Count);
        Assert.Equal(new DateOnly(2026, 11, 9), byDay[^1].Day);
        Assert.Equal(2, byDay[^1].Count);
        Assert.Equal(3, byDay.Sum(d => d.Count));
    }

    [Fact]
    public void GetStats_ranks_referrers_by_host_and_labels_missing_as_direct()
    {
        AddLink("abc0000", Now);
        AddClick("abc0000", Now, "https://google.com/search?q=x");
        AddClick("abc0000", Now, "https://google.com/other");
        AddClick("abc0000", Now, "https://twitter.com/");
        AddClick("abc0000", Now, null);

        var top = _sut.GetStats("abc0000")!.TopReferrers;

        Assert.Equal(("google.com", 2), (top[0].Referrer, top[0].Count));
        Assert.Contains(top, r => r.Referrer == "twitter.com" && r.Count == 1);
        Assert.Contains(top, r => r.Referrer == "direct" && r.Count == 1);
    }

    [Fact]
    public void GetStats_limits_top_referrers_to_ten()
    {
        AddLink("abc0000", Now);
        for (var i = 0; i < 12; i++) AddClick("abc0000", Now, $"https://site{i}.com");

        Assert.Equal(10, _sut.GetStats("abc0000")!.TopReferrers.Count);
    }
}
