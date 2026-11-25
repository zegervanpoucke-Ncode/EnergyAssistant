using System.Security.Cryptography;
using System.Text;

namespace EnergyAssistant.Api.Infrastructure.Fluvius.Authentication;

public static class PkceHelper
{
    public static string GenerateVerifier()
    {
        return Base64UrlEncode(
            RandomNumberGenerator.GetBytes(32));
    }

    public static string GenerateChallenge(string verifier)
    {
        var bytes = Encoding.ASCII.GetBytes(verifier);
        var hash = SHA256.HashData(bytes);

        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}