using EnergyAssistant.Api.Infrastructure.Fluvius.Authentication;

public sealed class StubFluviusTokenRefresher
    : IFluviusTokenRefresher
{
    public Task<FluviusTokens> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "Automatic Fluvius token refresh is not configured.");
    }
}