using UrlShortener.Api.Contracts;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapPost("/shorten", Shorten);
        api.MapGet("/urls", ListUrls);
        api.MapGet("/stats/{code}", GetStats);

        return app;
    }

    private static IResult Shorten(
        ShortenRequest body,
        HttpRequest request,
        UrlShortenerService shortener,
        IShortUrlBuilder urlBuilder)
    {
        var result = shortener.Create(body.Url, body.ExpiresAt);
        if (!result.IsSuccess)
            return Results.BadRequest(new { error = "url must be an absolute http:// or https:// URL" });

        var link = result.Value!;
        var response = ShortLinkResponse.From(link, urlBuilder.Build(link.ShortCode, request));
        return Results.Created($"/api/stats/{link.ShortCode}", response);
    }

    private static IResult ListUrls(HttpRequest request, LinkAnalyticsService analytics, IShortUrlBuilder urlBuilder) =>
        Results.Ok(analytics.ListLinks()
            .Select(s => LinkSummaryResponse.From(s, urlBuilder.Build(s.Link.ShortCode, request)))
            .ToList());

    private static IResult GetStats(string code, LinkAnalyticsService analytics)
    {
        var stats = analytics.GetStats(code);
        return stats is null ? Results.NotFound() : Results.Ok(LinkStatsResponse.From(stats));
    }
}
