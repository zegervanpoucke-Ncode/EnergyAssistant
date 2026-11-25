namespace EnergyAssistant.Api.Infrastructure.Fluvius.Authentication;

public sealed class FluviusTokenProvider(
    IFluviusTokenStore tokenStore,
    IFluviusTokenRefresher tokenRefresher)
    : IFluviusTokenProvider
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<string> GetAccessTokenAsync(
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var tokens = await tokenStore.GetAsync(
                cancellationToken);

            if (tokens is null)
            {
                throw new InvalidOperationException(
                    "No Fluvius authentication tokens available.");
            }

            if (tokens.ExpiresAt >
                DateTimeOffset.UtcNow.AddMinutes(2))
            {
                return tokens.AccessToken;
            }

            if (string.IsNullOrWhiteSpace(tokens.RefreshToken))
            {
                throw new InvalidOperationException(
                    "Fluvius session expired. Login required.");
            }

            var refreshedTokens =
                await tokenRefresher.RefreshAsync(
                    tokens.RefreshToken,
                    cancellationToken);

            await tokenStore.SaveAsync(
                refreshedTokens,
                cancellationToken);

            return refreshedTokens.AccessToken;
        }
        finally
        {
            _lock.Release();
        }
    }
}