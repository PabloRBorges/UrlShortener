namespace UrlShortener.Api.Domain;

public sealed record ShortLink(
    string ShortCode,
    string OriginalUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt)
{
    public bool IsExpiredAt(DateTimeOffset now) => ExpiresAt is { } exp && exp <= now;
}
