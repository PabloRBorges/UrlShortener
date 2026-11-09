namespace UrlShortener.Api.Services;

public interface IShortUrlBuilder
{
    string Build(string shortCode, HttpRequest request);
}

/// <summary>Uses the configured BaseUrl; falls back to the incoming request's scheme and host.</summary>
public sealed class ConfiguredShortUrlBuilder(IConfiguration configuration) : IShortUrlBuilder
{
    public string Build(string shortCode, HttpRequest request)
    {
        var baseUrl = configuration["BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = $"{request.Scheme}://{request.Host}";

        return $"{baseUrl.TrimEnd('/')}/{shortCode}";
    }
}
