namespace UrlShortener.Api.Services;

public sealed class HttpUrlValidator : IUrlValidator
{
    public bool IsValid(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
