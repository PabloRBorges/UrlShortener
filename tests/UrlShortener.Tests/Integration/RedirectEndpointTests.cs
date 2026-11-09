using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace UrlShortener.Tests.Integration;

public class RedirectEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public RedirectEndpointTests(ApiFactory factory) =>
        _client = factory.CreateClient(new() { AllowAutoRedirect = false });

    private async Task<string> Shorten(string url, DateTimeOffset? expiresAt = null)
    {
        var response = await _client.PostAsJsonAsync("/api/shorten", new { url, expiresAt });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("shortCode").GetString()!;
    }

    [Fact]
    public async Task Get_code_redirects_301_to_original_url()
    {
        var code = await Shorten("https://example.com/page");

        var response = await _client.GetAsync($"/{code}");

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("https://example.com/page", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Get_unknown_code_returns_404()
    {
        var response = await _client.GetAsync("/zzzzzzz");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_expired_code_returns_410()
    {
        var code = await Shorten("https://example.com", ApiFactory.Now.AddMinutes(-1));

        var response = await _client.GetAsync($"/{code}");

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
    }

    [Fact]
    public async Task Get_code_records_click_with_referrer()
    {
        var code = await Shorten("https://example.com");
        var request = new HttpRequestMessage(HttpMethod.Get, $"/{code}");
        request.Headers.Referrer = new Uri("https://news.ycombinator.com/item?id=1");
        request.Headers.UserAgent.ParseAdd("TestAgent/1.0");

        await _client.SendAsync(request);

        var stats = await _client.GetFromJsonAsync<JsonElement>($"/api/stats/{code}");
        Assert.Equal(1, stats.GetProperty("totalClicks").GetInt32());
        var top = stats.GetProperty("topReferrers")[0];
        Assert.Equal("news.ycombinator.com", top.GetProperty("referrer").GetString());
    }
}
