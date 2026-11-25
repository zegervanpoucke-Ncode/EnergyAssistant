
using EnergyAssistant.Api.Infrastructure.Fluvius.Authentication;
using FakeItEasy;

namespace EnergyAssistant.Tests.Infrastructure.Fluvius.Authentication;

public sealed class FluviusTokenProviderTests
{
    private readonly IFluviusTokenStore _tokenStore;
    private readonly IFluviusTokenRefresher _tokenRefresher;
    private readonly FluviusTokenProvider _sut;

    public FluviusTokenProviderTests()
    {
        _tokenStore = A.Fake<IFluviusTokenStore>();
        _tokenRefresher = A.Fake<IFluviusTokenRefresher>();

        _sut = new FluviusTokenProvider(
            _tokenStore,
            _tokenRefresher);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenTokenIsValid_ReturnsExistingToken()
    {
        // Arrange
        var tokens = new FluviusTokens(
            AccessToken: "valid-access-token",
            RefreshToken: "refresh-token",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1));

        A.CallTo(() => _tokenStore.GetAsync(
                A<CancellationToken>._))
            .Returns(tokens);

        // Act
        var result = await _sut.GetAccessTokenAsync();

        // Assert
        Assert.Equal("valid-access-token", result);

        A.CallTo(() => _tokenRefresher.RefreshAsync(
                A<string>._,
                A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenTokenIsExpired_RefreshesAndStoresNewToken()
    {
        // Arrange
        var expiredTokens = new FluviusTokens(
            AccessToken: "expired-access-token",
            RefreshToken: "old-refresh-token",
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(-10));

        var refreshedTokens = new FluviusTokens(
            AccessToken: "new-access-token",
            RefreshToken: "new-refresh-token",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1));

        A.CallTo(() => _tokenStore.GetAsync(
                A<CancellationToken>._))
            .Returns(expiredTokens);

        A.CallTo(() => _tokenRefresher.RefreshAsync(
                "old-refresh-token",
                A<CancellationToken>._))
            .Returns(refreshedTokens);

        // Act
        var result = await _sut.GetAccessTokenAsync();

        // Assert
        Assert.Equal("new-access-token", result);

        A.CallTo(() => _tokenRefresher.RefreshAsync(
                "old-refresh-token",
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => _tokenStore.SaveAsync(
                refreshedTokens,
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenRefreshTokenIsMissing_ThrowsException()
    {
        // Arrange
        var expiredTokens = new FluviusTokens(
            AccessToken: "expired-access-token",
            RefreshToken: null,
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(-10));

        A.CallTo(() => _tokenStore.GetAsync(
                A<CancellationToken>._))
            .Returns(expiredTokens);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GetAccessTokenAsync());

        // Assert
        Assert.Equal(
            "Fluvius session expired. Login required.",
            exception.Message);

        A.CallTo(() => _tokenRefresher.RefreshAsync(
                A<string>._,
                A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenNoTokensExist_ThrowsException()
    {
        // Arrange
        A.CallTo(() => _tokenStore.GetAsync(
                A<CancellationToken>._))
            .Returns((FluviusTokens?)null);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GetAccessTokenAsync());

        // Assert
        Assert.Equal(
            "No Fluvius authentication tokens available.",
            exception.Message);
    }
}
