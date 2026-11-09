using UrlShortener.Api.Domain;

namespace UrlShortener.Api.Data;

public interface IUrlRepository
{
    /// <summary>Adds the link. Returns false when the short code is already taken.</summary>
    bool TryAdd(ShortLink link);
    ShortLink? Find(string shortCode);
    void AddClick(Click click);
    IReadOnlyList<Click> GetClicks(string shortCode);
    IReadOnlyList<ShortLink> ListAll();
}
