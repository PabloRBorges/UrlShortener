using UrlShortener.Api.Domain;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Endpoints;

public static class RedirectEndpoints
{
    public static IEndpointRouteBuilder MapRedirectEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{code}", Redirect);
        return app;
    }

    private static IResult Redirect(string code, HttpContext http, UrlShortenerService shortener)
    {
        var result = shortener.Resolve(code, ClickContextFrom(http));

        return result.Status switch
        {
            ResolveStatus.Found => Results.Redirect(result.OriginalUrl!, permanent: true),
            ResolveStatus.Expired => Results.StatusCode(StatusCodes.Status410Gone),
            _ => Results.NotFound(),
        };
    }

    private static ClickContext ClickContextFrom(HttpContext http)
    {
        var headers = http.Request.Headers;
        var forwardedFor = headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim();
        var ip = string.IsNullOrEmpty(forwardedFor) ? http.Connection.RemoteIpAddress?.ToString() : forwardedFor;

        return new ClickContext(
            Referrer: headers.Referer.FirstOrDefault(),
            UserAgent: headers.UserAgent.FirstOrDefault(),
            Ip: ip);
    }
}
