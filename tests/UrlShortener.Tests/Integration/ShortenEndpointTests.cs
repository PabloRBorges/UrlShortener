using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace UrlShortener.Tests.Integration;

public class ShortenEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ShortenEndpointTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Post_shorten_returns_201_with_link_contract()
    {
        var response = await _client.PostAsJsonAsync("/api/shorten", new { url = "https://example.com" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var code = body.GetProperty("shortCode").GetString()!;
        Assert.Matches("^[a-zA-Z0-9]{7}$", code);
        Assert.Equal($"http://short.test/{code}", body.GetProperty("shortUrl").GetString());
        Assert.Equal("https://example.com", body.GetProperty("originalUrl").GetString());
        Assert.Equal(ApiFactory.Now, body.GetProperty("createdAt").GetDateTimeOffset());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("expiresAt").ValueKind);
    }

    [Fact]
    public async Task Post_shorten_echoes_expiresAt()
    {
        var expires = ApiFactory.Now.AddDays(1);

        var response = await _client.PostAsJsonAsync("/api/shorten", new { url = "https://example.com", expiresAt = expires });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expires, body.GetProperty("expiresAt").GetDateTimeOffset());
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("not a url")]
    [InlineData("")]
    public async Task Post_shorten_returns_400_for_invalid_url(string url)
    {
        var response = await _client.PostAsJsonAsync("/api/shorten", new { url });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_shorten_returns_400_for_missing_url()
    {
        var response = await _client.PostAsync("/api/shorten", new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
