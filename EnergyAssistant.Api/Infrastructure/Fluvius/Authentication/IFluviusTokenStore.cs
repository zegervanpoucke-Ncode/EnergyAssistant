namespace EnergyAssistant.Api.Infrastructure.Fluvius.Authentication;

public interface IFluviusTokenStore
{
    Task<FluviusTokens?> GetAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        FluviusTokens tokens,
        CancellationToken cancellationToken = default);
}