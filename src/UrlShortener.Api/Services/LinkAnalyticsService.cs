using UrlShortener.Api.Data;
using UrlShortener.Api.Domain;

namespace UrlShortener.Api.Services;

public sealed class LinkAnalyticsService(IUrlRepository repository, IClock clock)
{
    private const int WindowDays = 30;
    private const int TopReferrersLimit = 10;
    private const string DirectReferrer = "direct";

    public IReadOnlyList<LinkSummary> ListLinks() =>
        repository.ListAll()
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new LinkSummary(l, repository.GetClicks(l.ShortCode).Count))
            .ToList();

    public LinkStats? GetStats(string shortCode)
    {
        var link = repository.Find(shortCode);
        if (link is null) return null;

        var clicks = repository.GetClicks(shortCode);
        return new LinkStats(link, clicks.Count, ClicksByDay(clicks), TopReferrers(clicks));
    }

    private IReadOnlyList<DailyClicks> ClicksByDay(IReadOnlyList<Click> clicks)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var firstDay = today.AddDays(-(WindowDays - 1));

        var counts = clicks
            .Select(c => DateOnly.FromDateTime(c.Timestamp.UtcDateTime))
            .Where(d => d >= firstDay && d <= today)
            .GroupBy(d => d)
            .ToDictionary(g => g.Key, g => g.Count());

        return Enumerable.Range(0, WindowDays)
            .Select(i => firstDay.AddDays(i))
            .Select(day => new DailyClicks(day, counts.GetValueOrDefault(day)))
            .ToList();
    }

    private static IReadOnlyList<ReferrerClicks> TopReferrers(IReadOnlyList<Click> clicks) =>
        clicks
            .GroupBy(c => NormalizeReferrer(c.Referrer))
            .Select(g => new ReferrerClicks(g.Key, g.Count()))
            .OrderByDescending(r => r.Count)
            .ThenBy(r => r.Referrer)
            .Take(TopReferrersLimit)
            .ToList();

    private static string NormalizeReferrer(string? referrer) =>
        Uri.TryCreate(referrer, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host)
            ? uri.Host
            : string.IsNullOrWhiteSpace(referrer) ? DirectReferrer : referrer;
}
