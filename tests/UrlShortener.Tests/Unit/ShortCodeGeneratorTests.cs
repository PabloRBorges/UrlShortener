using System.Text.RegularExpressions;
using UrlShortener.Api.Services;

namespace UrlShortener.Tests.Unit;

public class ShortCodeGeneratorTests
{
    private readonly RandomShortCodeGenerator _sut = new();

    [Fact]
    public void Generate_returns_seven_alphanumeric_characters()
    {
        var code = _sut.Generate();

        Assert.Equal(7, code.Length);
        Assert.Matches("^[a-zA-Z0-9]{7}$", code);
    }

    [Fact]
    public void Generate_returns_different_codes_on_successive_calls()
    {
        var codes = Enumerable.Range(0, 50).Select(_ => _sut.Generate()).ToHashSet();

        Assert.Equal(50, codes.Count);
    }
}
