using UrlShortener.Api.Data;
using UrlShortener.Api.Domain;
using UrlShortener.Api.Services;
using UrlShortener.Tests.Fakes;

namespace UrlShortener.Tests.Unit;

public class UrlShortenerServiceResolveTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);
    private static readonly ClickContext Ctx = new("https://google.com", "Mozilla/5.0", "10.0.0.1");

    private readonly InMemoryUrlRepository _repo = new();
    private readonly FakeClock _clock = new(Now);
    private readonly UrlShortenerService _sut;

    public UrlShortenerServiceResolveTests() =>
        _sut = new(_repo, new SequenceShortCodeGenerator("code001"), new HttpUrlValidator(), _clock);

    [Fact]
    public void Resolve_returns_not_found_for_unknown_code()
    {
        var result = _sut.Resolve("nope000", Ctx);

        Assert.Equal(ResolveStatus.NotFound, result.Status);
    }

    [Fact]
    public void Resolve_returns_original_url_and_records_click()
    {
        _sut.Create("https://example.com", null);

        var result = _sut.Resolve("code001", Ctx);

        Assert.Equal(ResolveStatus.Found, result.Status);
        Assert.Equal("https://example.com", result.OriginalUrl);
        var click = Assert.Single(_repo.GetClicks("code001"));
        Assert.Equal("https://google.com", click.Referrer);
        Assert.Equal("Mozilla/5.0", click.UserAgent);
        Assert.Equal("10.0.0.1", click.Ip);
        Assert.Equal(Now, click.Timestamp);
    }

    [Fact]
    public void Resolve_returns_expired_and_does_not_record_click_after_expiration()
    {
        _sut.Create("https://example.com", Now.AddHours(1));
        _clock.UtcNow = Now.AddHours(2);

        var result = _sut.Resolve("code001", Ctx);

        Assert.Equal(ResolveStatus.Expired, result.Status);
        Assert.Empty(_repo.GetClicks("code001"));
    }

    [Fact]
    public void Resolve_before_expiration_is_found()
    {
        _sut.Create("https://example.com", Now.AddHours(1));
        _clock.UtcNow = Now.AddMinutes(59);

        Assert.Equal(ResolveStatus.Found, _sut.Resolve("code001", Ctx).Status);
    }
}
