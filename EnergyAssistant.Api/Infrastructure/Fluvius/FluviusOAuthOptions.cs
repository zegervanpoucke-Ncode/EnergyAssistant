namespace EnergyAssistant.Api.Infrastructure.Fluvius.Authentication;

public sealed class FluviusOAuthOptions
{
    public const string SectionName = "Fluvius:OAuth";

    public string Authority { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public string[] Scopes { get; set; } = [];
    public string TokenEndpoint { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
}