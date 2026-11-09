using System.Security.Cryptography;

namespace UrlShortener.Api.Services;

public sealed class RandomShortCodeGenerator : IShortCodeGenerator
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int Length = 7;

    public string Generate() => RandomNumberGenerator.GetString(Alphabet, Length);
}
