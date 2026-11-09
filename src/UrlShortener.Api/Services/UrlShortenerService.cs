using UrlShortener.Api.Data;
using UrlShortener.Api.Domain;

namespace UrlShortener.Api.Services;

public sealed class UrlShortenerService(
    IUrlRepository repository,
    IShortCodeGenerator codeGenerator,
    IUrlValidator urlValidator,
    IClock clock)
{
    private const int MaxCollisionRetries = 5;

    public CreateLinkResult Create(string? url, DateTimeOffset? expiresAt)
    {
        if (!urlValidator.IsValid(url))
            return CreateLinkResult.Fail(CreateLinkError.InvalidUrl);

        for (var attempt = 0; attempt < MaxCollisionRetries; attempt++)
        {
            var link = new ShortLink(codeGenerator.Generate(), url!, clock.UtcNow, expiresAt);
            if (repository.TryAdd(link))
                return CreateLinkResult.Ok(link);
        }

        throw new InvalidOperationException("Could not allocate a unique short code.");
    }

    public ResolveResult Resolve(string shortCode, ClickContext context)
    {
        var link = repository.Find(shortCode);
        if (link is null) return ResolveResult.NotFound;

        var now = clock.UtcNow;
        if (link.IsExpiredAt(now)) return ResolveResult.Expired;

        repository.AddClick(new Click(shortCode, context.Referrer, context.UserAgent, context.Ip, now));
        return ResolveResult.Found(link.OriginalUrl);
    }
}
