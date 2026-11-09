namespace UrlShortener.Api.Services;

public interface IUrlValidator
{
    bool IsValid(string? url);
}
