namespace EnergyAssistant.Api.Infrastructure.Fluvius.Authentication;

public sealed record FluviusTokens(
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset ExpiresAt);