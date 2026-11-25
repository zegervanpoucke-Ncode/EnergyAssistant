namespace EnergyAssistant.Api.Infrastructure.Fluvius.Authentication;

public interface IFluviusTokenRefresher
{
    Task<FluviusTokens> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}