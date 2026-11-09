using UrlShortener.Api.Services;

namespace UrlShortener.Tests.Unit;

public class UrlValidatorTests
{
    private readonly HttpUrlValidator _sut = new();

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/path?q=1")]
    public void IsValid_accepts_http_and_https_absolute_urls(string url)
    {
        Assert.True(_sut.IsValid(url));
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("example.com")]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_rejects_non_http_or_malformed_urls(string url)
    {
        Assert.False(_sut.IsValid(url));
    }
}
