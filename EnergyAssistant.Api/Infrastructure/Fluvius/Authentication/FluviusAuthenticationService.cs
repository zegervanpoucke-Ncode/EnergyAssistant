
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace EnergyAssistant.Api.Infrastructure.Fluvius.Authentication;

public sealed record FluviusLoginAttempt(
    string AuthorizationUrl,
    string CodeVerifier,
    string State);

public sealed class FluviusAuthenticationService(
    HttpClient httpClient)
{
    private const string ConfigUrl =
        "https://mijn.fluvius.be/api/global/msal/config";

    private const string DefaultScope =
        "https://klanten.onmicrosoft.com/MijnFluvius/user_impersonation";

    public async Task<FluviusLoginAttempt> StartLoginAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            ConfigUrl,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        using var document = await response.Content
            .ReadFromJsonAsync<JsonDocument>(
                cancellationToken);

        if (document is null)
        {
            throw new InvalidOperationException(
                "Fluvius MSAL configuration is empty.");
        }

        var config = document.RootElement;

        var clientId = GetSetting(config, "clientId");
        var authority = GetSetting(config, "authority");
        var redirectUri = GetSetting(config, "redirectUri");

        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(authority) ||
            string.IsNullOrWhiteSpace(redirectUri))
        {
            throw new InvalidOperationException(
                "Fluvius MSAL configuration is incomplete.");
        }

        // Accept only the expected Fluvius identity host.
        var authorityUri = new Uri(authority);

        if (authorityUri.Scheme != Uri.UriSchemeHttps ||
            !authorityUri.Host.Equals(
                "login.fluvius.be",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Unexpected Fluvius OAuth authority.");
        }

        var verifier = GenerateRandomValue(64);
        var challenge = GenerateChallenge(verifier);
        var state = GenerateRandomValue(32);
        var nonce = GenerateRandomValue(32);

        var parameters = new Dictionary<string, string?>
        {
            ["client_id"] = clientId,
            ["response_type"] = "code",
            ["response_mode"] = "query",
            ["redirect_uri"] = redirectUri,
            ["scope"] =
                $"openid offline_access {DefaultScope}",
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state,
            ["nonce"] = nonce,
            ["prompt"] = "login"
        };

        var url = QueryHelpers.AddQueryString(
            $"{authority.TrimEnd('/')}/oauth2/v2.0/authorize",
            parameters);

        return new FluviusLoginAttempt(
            url,
            verifier,
            state);
    }

    private static string? GetSetting(
        JsonElement config,
        string name)
    {
        if (config.TryGetProperty(name, out var value))
        {
            return value.GetString();
        }

        if (config.TryGetProperty("auth", out var auth) &&
            auth.TryGetProperty(name, out value))
        {
            return value.GetString();
        }

        return null;
    }

    private static string GenerateRandomValue(int byteCount)
    {
        return Base64UrlEncode(
            RandomNumberGenerator.GetBytes(byteCount));
    }

    private static string GenerateChallenge(string verifier)
    {
        var hash = SHA256.HashData(
            Encoding.ASCII.GetBytes(verifier));

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
