namespace UrlShortener.Api.Domain;

public sealed record LinkSummary(ShortLink Link, int ClickCount);

public sealed record DailyClicks(DateOnly Day, int Count);

public sealed record ReferrerClicks(string Referrer, int Count);

public sealed record LinkStats(
    ShortLink Link,
    int TotalClicks,
    IReadOnlyList<DailyClicks> ClicksByDay,
    IReadOnlyList<ReferrerClicks> TopReferrers);
