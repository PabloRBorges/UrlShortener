using System.Collections.Concurrent;
using UrlShortener.Api.Domain;

namespace UrlShortener.Api.Data;

public sealed class InMemoryUrlRepository : IUrlRepository
{
    private readonly ConcurrentDictionary<string, ShortLink> _links = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentQueue<Click>> _clicks = new(StringComparer.Ordinal);

    public bool TryAdd(ShortLink link) => _links.TryAdd(link.ShortCode, link);

    public ShortLink? Find(string shortCode) => _links.GetValueOrDefault(shortCode);

    public void AddClick(Click click) =>
        _clicks.GetOrAdd(click.ShortCode, _ => new ConcurrentQueue<Click>()).Enqueue(click);

    public IReadOnlyList<Click> GetClicks(string shortCode) =>
        _clicks.TryGetValue(shortCode, out var q) ? q.ToArray() : [];

    public IReadOnlyList<ShortLink> ListAll() => _links.Values.ToArray();
}
