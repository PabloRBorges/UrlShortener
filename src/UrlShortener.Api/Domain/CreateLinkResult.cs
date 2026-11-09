namespace UrlShortener.Api.Domain;

public enum CreateLinkError { InvalidUrl }

public sealed record CreateLinkResult(ShortLink? Value, CreateLinkError? Error)
{
    public bool IsSuccess => Error is null;
    public static CreateLinkResult Ok(ShortLink link) => new(link, null);
    public static CreateLinkResult Fail(CreateLinkError error) => new(null, error);
}
