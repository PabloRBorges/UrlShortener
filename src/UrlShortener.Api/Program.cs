using UrlShortener.Api.Data;
using UrlShortener.Api.Endpoints;
using UrlShortener.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IUrlRepository, InMemoryUrlRepository>();
builder.Services.AddSingleton<IShortCodeGenerator, RandomShortCodeGenerator>();
builder.Services.AddSingleton<IUrlValidator, HttpUrlValidator>();
builder.Services.AddSingleton<IShortUrlBuilder, ConfiguredShortUrlBuilder>();
builder.Services.AddSingleton<UrlShortenerService>();
builder.Services.AddSingleton<LinkAnalyticsService>();

var app = builder.Build();

app.MapApiEndpoints();
app.MapRedirectEndpoints();
app.MapAnalyticsPage();

app.Run();

public partial class Program;
