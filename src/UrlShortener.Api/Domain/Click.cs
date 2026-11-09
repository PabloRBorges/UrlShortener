namespace UrlShortener.Api.Domain;

/// <summary>Request metadata captured on each redirect.</summary>
public sealed record ClickContext(string? Referrer, string? UserAgent, string? Ip);

public sealed record Click(
    string ShortCode,
    string? Referrer,
    string? UserAgent,
    string? Ip,
    DateTimeOffset Timestamp);
