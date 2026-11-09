using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Api.Services;
using UrlShortener.Tests.Fakes;

namespace UrlShortener.Tests.Integration;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public static readonly DateTimeOffset Now = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);
    public FakeClock Clock { get; } = new(Now);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("BaseUrl", "http://short.test");
        builder.ConfigureServices(services => services.AddSingleton<IClock>(Clock));
    }
}
