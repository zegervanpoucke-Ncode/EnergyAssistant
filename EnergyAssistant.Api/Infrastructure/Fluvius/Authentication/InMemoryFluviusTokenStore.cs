namespace EnergyAssistant.Api.Infrastructure.Fluvius.Authentication;

public sealed class InMemoryFluviusTokenStore
    : IFluviusTokenStore
{
    private FluviusTokens? _tokens;

    public Task<FluviusTokens?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_tokens);
    }

    public Task SaveAsync(
        FluviusTokens tokens,
        CancellationToken cancellationToken = default)
    {
        _tokens = tokens;
        return Task.CompletedTask;
    }
}