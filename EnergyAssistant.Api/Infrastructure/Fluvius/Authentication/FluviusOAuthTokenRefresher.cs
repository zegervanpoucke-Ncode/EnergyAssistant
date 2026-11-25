
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace EnergyAssistant.Api.Infrastructure.Fluvius.Authentication;

public sealed class FluviusOAuthTokenRefresher(
    HttpClient httpClient,
    IOptions<FluviusOAuthOptions> options)
    : IFluviusTokenRefresher
{
    private readonly FluviusOAuthOptions _options = options.Value;

    public async Task<FluviusTokens> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException(
                "Refresh token is required.",
                nameof(refreshToken));
        }

        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = _options.ClientId
        };

        if (!string.IsNullOrWhiteSpace(_options.Scope))
        {
            parameters["scope"] = _options.Scope;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            _options.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(parameters)
        };

        using var response = await httpClient.SendAsync(
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Fluvius token refresh failed: {(int)response.StatusCode}");
        }

        var result = await response.Content
            .ReadFromJsonAsync<TokenResponse>(
                cancellationToken: cancellationToken);

        if (result is null ||
            string.IsNullOrWhiteSpace(result.AccessToken) ||
            result.ExpiresIn <= 0)
        {
            throw new InvalidOperationException(
                "Invalid Fluvius token response.");
        }

        return new FluviusTokens(
            AccessToken: result.AccessToken,
            RefreshToken: string.IsNullOrWhiteSpace(result.RefreshToken)
                ? refreshToken
                : result.RefreshToken,
            ExpiresAt: DateTimeOffset.UtcNow
                .AddSeconds(result.ExpiresIn));
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
