using UrlShortener.Api.Services;

namespace UrlShortener.Tests.Fakes;

public sealed class FakeClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;
}
