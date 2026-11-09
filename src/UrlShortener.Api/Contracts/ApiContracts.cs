using UrlShortener.Api.Domain;

namespace UrlShortener.Api.Contracts;

public sealed record ShortenRequest(string? Url, DateTimeOffset? ExpiresAt);

public sealed record ShortLinkResponse(
    string ShortCode,
    string ShortUrl,
    string OriginalUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt)
{
    public static ShortLinkResponse From(ShortLink link, string shortUrl) =>
        new(link.ShortCode, shortUrl, link.OriginalUrl, link.CreatedAt, link.ExpiresAt);
}

public sealed record LinkSummaryResponse(
    string ShortCode,
    string ShortUrl,
    string OriginalUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    int ClickCount)
{
    public static LinkSummaryResponse From(LinkSummary summary, string shortUrl) =>
        new(summary.Link.ShortCode, shortUrl, summary.Link.OriginalUrl,
            summary.Link.CreatedAt, summary.Link.ExpiresAt, summary.ClickCount);
}

public sealed record DailyClicksResponse(string Day, int Count);

public sealed record ReferrerClicksResponse(string Referrer, int Count);

public sealed record LinkStatsResponse(
    string ShortCode,
    string OriginalUrl,
    int TotalClicks,
    IReadOnlyList<DailyClicksResponse> ClicksByDay,
    IReadOnlyList<ReferrerClicksResponse> TopReferrers)
{
    public static LinkStatsResponse From(LinkStats stats) =>
        new(stats.Link.ShortCode,
            stats.Link.OriginalUrl,
            stats.TotalClicks,
            stats.ClicksByDay.Select(d => new DailyClicksResponse(d.Day.ToString("yyyy-MM-dd"), d.Count)).ToList(),
            stats.TopReferrers.Select(r => new ReferrerClicksResponse(r.Referrer, r.Count)).ToList());
}
