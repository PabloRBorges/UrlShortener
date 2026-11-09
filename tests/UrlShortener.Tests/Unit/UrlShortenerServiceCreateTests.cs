using UrlShortener.Api.Data;
using UrlShortener.Api.Domain;
using UrlShortener.Api.Services;
using UrlShortener.Tests.Fakes;

namespace UrlShortener.Tests.Unit;

public class UrlShortenerServiceCreateTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

    private static UrlShortenerService Build(IShortCodeGenerator? generator = null, IUrlRepository? repo = null) =>
        new(repo ?? new InMemoryUrlRepository(),
            generator ?? new RandomShortCodeGenerator(),
            new HttpUrlValidator(),
            new FakeClock(Now));

    [Fact]
    public void Create_stores_link_with_generated_code_and_creation_time()
    {
        var sut = Build(new SequenceShortCodeGenerator("abc1234"));

        var result = sut.Create("https://example.com", expiresAt: null);

        Assert.True(result.IsSuccess);
        var link = result.Value!;
        Assert.Equal("abc1234", link.ShortCode);
        Assert.Equal("https://example.com", link.OriginalUrl);
        Assert.Equal(Now, link.CreatedAt);
        Assert.Null(link.ExpiresAt);
    }

    [Fact]
    public void Create_keeps_expiration_when_provided()
    {
        var sut = Build();
        var expires = Now.AddDays(1);

        var result = sut.Create("https://example.com", expires);

        Assert.Equal(expires, result.Value!.ExpiresAt);
    }

    [Fact]
    public void Create_fails_for_invalid_url()
    {
        var sut = Build();

        var result = sut.Create("ftp://example.com", null);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateLinkError.InvalidUrl, result.Error);
    }

    [Fact]
    public void Create_retries_when_generated_code_collides()
    {
        var repo = new InMemoryUrlRepository();
        var sut = Build(new SequenceShortCodeGenerator("dup0000", "dup0000", "new0000"), repo);

        var first = sut.Create("https://a.com", null);
        var second = sut.Create("https://b.com", null);

        Assert.Equal("dup0000", first.Value!.ShortCode);
        Assert.Equal("new0000", second.Value!.ShortCode);
    }
}
