using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace UrlShortener.Tests.Integration;

public class ListAndStatsEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ListAndStatsEndpointTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    private async Task<string> Shorten(string url)
    {
        var response = await _client.PostAsJsonAsync("/api/shorten", new { url });
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("shortCode").GetString()!;
    }

    [Fact]
    public async Task Get_urls_lists_newest_first_with_click_count()
    {
        var first = await Shorten("https://first.com");
        _factory.Clock.UtcNow = _factory.Clock.UtcNow.AddSeconds(1);
        var second = await Shorten("https://second.com");
        await _client.GetAsync($"/{first}");

        var list = await _client.GetFromJsonAsync<JsonElement>("/api/urls");

        var items = list.EnumerateArray().ToList();
        var idxFirst = items.FindIndex(i => i.GetProperty("shortCode").GetString() == first);
        var idxSecond = items.FindIndex(i => i.GetProperty("shortCode").GetString() == second);
        Assert.True(idxSecond < idxFirst, "newest link should come first");
        Assert.Equal(1, items[idxFirst].GetProperty("clickCount").GetInt32());
        Assert.Equal(0, items[idxSecond].GetProperty("clickCount").GetInt32());
    }

    [Fact]
    public async Task Get_stats_returns_totals_by_day_and_referrers()
    {
        var code = await Shorten("https://example.com");
        await _client.GetAsync($"/{code}");
        await _client.GetAsync($"/{code}");

        var stats = await _client.GetFromJsonAsync<JsonElement>($"/api/stats/{code}");

        Assert.Equal(code, stats.GetProperty("shortCode").GetString());
        Assert.Equal(2, stats.GetProperty("totalClicks").GetInt32());
        var byDay = stats.GetProperty("clicksByDay").EnumerateArray().ToList();
        Assert.Equal(30, byDay.Count);
        Assert.Equal(2, byDay[^1].GetProperty("count").GetInt32());
        Assert.Equal("direct", stats.GetProperty("topReferrers")[0].GetProperty("referrer").GetString());
    }

    [Fact]
    public async Task Get_stats_unknown_code_returns_404()
    {
        var response = await _client.GetAsync("/api/stats/zzzzzzz");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_analytics_page_returns_html_with_code()
    {
        var code = await Shorten("https://example.com");

        var response = await _client.GetAsync($"/analytics/{code}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("text/html", response.Content.Headers.ContentType!.MediaType);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains(code, html);
        Assert.Contains($"/api/stats/{code}", html);
    }

    [Fact]
    public async Task Get_analytics_page_unknown_code_returns_404()
    {
        var response = await _client.GetAsync("/analytics/zzzzzzz");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
