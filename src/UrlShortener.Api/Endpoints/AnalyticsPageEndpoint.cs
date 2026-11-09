using UrlShortener.Api.Pages;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Endpoints;

public static class AnalyticsPageEndpoint
{
    public static IEndpointRouteBuilder MapAnalyticsPage(this IEndpointRouteBuilder app)
    {
        app.MapGet("/analytics/{code}", (string code, HttpRequest request, LinkAnalyticsService analytics, IShortUrlBuilder urlBuilder) =>
        {
            var stats = analytics.GetStats(code);
            if (stats is null) return Results.NotFound();

            var html = AnalyticsPage.Render(stats.Link, urlBuilder.Build(code, request));
            return Results.Content(html, "text/html; charset=utf-8");
        });

        return app;
    }
}
