
using System.Net;
using System.Text;
using EnergyAssistant.Api.Infrastructure.Fluvius.Authentication;
using Microsoft.Extensions.Options;

namespace EnergyAssistant.Tests.Infrastructure.Fluvius.Authentication;

public sealed class FluviusOAuthTokenRefresherTests
{
    private const string TokenEndpoint =
        "https://login.example.test/oauth2/v2.0/token";

    private static FluviusOAuthTokenRefresher CreateSut(
        HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);

        var options = Options.Create(new FluviusOAuthOptions
        {
            TokenEndpoint = TokenEndpoint,
            ClientId = "test-client-id",
            Scope = "openid offline_access"
        });

        return new FluviusOAuthTokenRefresher(
            httpClient,
            options);
    }

    [Fact]
    public async Task RefreshAsync_WhenSuccessful_ReturnsNewAccessToken()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "access_token": "new-access-token",
                "refresh_token": "new-refresh-token",
                "expires_in": 3600
            }
            """);

        var sut = CreateSut(handler);

        // Act
        var result = await sut.RefreshAsync(
            "old-refresh-token");

        // Assert
        Assert.Equal("new-access-token", result.AccessToken);
        Assert.Equal("new-refresh-token", result.RefreshToken);

        Assert.InRange(
            result.ExpiresAt,
            DateTimeOffset.UtcNow.AddMinutes(59),
            DateTimeOffset.UtcNow.AddMinutes(61));
    }

    [Fact]
    public async Task RefreshAsync_SendsCorrectOAuthRequest()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "access_token": "new-access-token",
                "expires_in": 3600
            }
            """);

        var sut = CreateSut(handler);

        // Act
        await sut.RefreshAsync("old-refresh-token");

        // Assert
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal(TokenEndpoint, handler.RequestUri);
        Assert.NotNull(handler.ContentType);
        Assert.StartsWith(
            "application/x-www-form-urlencoded",
            handler.ContentType);

        var parameters = ParseForm(handler.RequestBody!);

        Assert.Equal("refresh_token", parameters["grant_type"]);
        Assert.Equal("old-refresh-token", parameters["refresh_token"]);
        Assert.Equal("test-client-id", parameters["client_id"]);
        Assert.Equal("openid offline_access", parameters["scope"]);
    }

    [Fact]
    public async Task RefreshAsync_WhenNoNewRefreshToken_KeepsExistingToken()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "access_token": "new-access-token",
                "expires_in": 3600
            }
            """);

        var sut = CreateSut(handler);

        // Act
        var result = await sut.RefreshAsync(
            "old-refresh-token");

        // Assert
        Assert.Equal("old-refresh-token", result.RefreshToken);
    }

    [Fact]
    public async Task RefreshAsync_WhenUnauthorized_ThrowsException()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(
            HttpStatusCode.Unauthorized,
            """
            {
                "error": "invalid_grant"
            }
            """);

        var sut = CreateSut(handler);

        // Act
        var exception = await Assert.ThrowsAsync<
            InvalidOperationException>(
                () => sut.RefreshAsync("invalid-refresh-token"));

        // Assert
        Assert.Contains("401", exception.Message);
    }

    [Fact]
    public async Task RefreshAsync_WhenResponseIsInvalid_ThrowsException()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "access_token": "",
                "expires_in": 0
            }
            """);

        var sut = CreateSut(handler);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RefreshAsync("old-refresh-token"));
    }

    private static Dictionary<string, string> ParseForm(string body)
    {
        return body.Split('&')
            .Select(part => part.Split('=', 2))
            .ToDictionary(
                pair => Uri.UnescapeDataString(
                    pair[0].Replace("+", " ")),
                pair => Uri.UnescapeDataString(
                    pair[1].Replace("+", " ")));
    }

    private sealed class TestHttpMessageHandler(
        HttpStatusCode statusCode,
        string responseBody) : HttpMessageHandler
    {
        public HttpMethod? RequestMethod { get; private set; }
        public string? RequestUri { get; private set; }
        public string? RequestBody { get; private set; }
        public string? ContentType { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestMethod = request.Method;
            RequestUri = request.RequestUri?.ToString();

            if (request.Content is not null)
            {
                RequestBody = await request.Content
                    .ReadAsStringAsync(cancellationToken);

                ContentType = request.Content.Headers.ContentType?
                    .ToString();
            }

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    responseBody,
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
