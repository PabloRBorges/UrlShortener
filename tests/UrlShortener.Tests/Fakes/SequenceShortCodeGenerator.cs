using UrlShortener.Api.Services;

namespace UrlShortener.Tests.Fakes;

/// <summary>Returns codes from a fixed sequence, so collision handling can be tested deterministically.</summary>
public sealed class SequenceShortCodeGenerator(params string[] codes) : IShortCodeGenerator
{
    private readonly Queue<string> _codes = new(codes);
    public string Generate() => _codes.Dequeue();
}
