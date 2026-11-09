namespace UrlShortener.Api.Domain;

public enum ResolveStatus { Found, NotFound, Expired }

public sealed record ResolveResult(ResolveStatus Status, string? OriginalUrl)
{
    public static ResolveResult Found(string url) => new(ResolveStatus.Found, url);
    public static readonly ResolveResult NotFound = new(ResolveStatus.NotFound, null);
    public static readonly ResolveResult Expired = new(ResolveStatus.Expired, null);
}
