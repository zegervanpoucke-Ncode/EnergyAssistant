using EnergyAssistant.Api.Features.HomeWizard;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace EnergyAssistant.Api.Infrastructure.HomeWizard;

internal sealed class HomeWizardClient(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<JsonElement> GetMeasurementAsync(CancellationToken cancellationToken)
    {
        var token = configuration["HomeWizard:Token"];
        if (!Uri.TryCreate(configuration["HomeWizard:BaseUrl"], UriKind.Absolute, out var baseUrl)
            || baseUrl.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrWhiteSpace(token)
            || string.IsNullOrWhiteSpace(configuration["HomeWizard:DeviceIdentity"]))
        {
            throw new HomeWizardReadException(HomeWizardReadFailure.InvalidConfiguration,
                "HomeWizard connection settings are incomplete or invalid.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get, new Uri(baseUrl, "/api/measurement"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Api-Version", "2");

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var measurement = await response.Content.ReadFromJsonAsync<JsonElement>(
                cancellationToken);
            if (measurement.ValueKind != JsonValueKind.Object)
            {
                throw new HomeWizardReadException(HomeWizardReadFailure.MeterError,
                    "The P1 Meter returned an invalid measurement.");
            }

            return measurement;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new HomeWizardReadException(HomeWizardReadFailure.Timeout,
                "The P1 Meter did not respond in time.", exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            var detail = exception is HttpRequestException
                { StatusCode: System.Net.HttpStatusCode.Unauthorized }
                ? "The P1 Meter rejected the configured HomeWizard token."
                : "Could not read a measurement from the P1 Meter.";

            throw new HomeWizardReadException(HomeWizardReadFailure.MeterError, detail, exception);
        }
    }

    public static HttpClientHandler CreateHandler(IConfiguration configuration)
    {
        var expectedIdentity = configuration["HomeWizard:DeviceIdentity"];

        return new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
            {
                if (certificate is null || string.IsNullOrWhiteSpace(expectedIdentity)
                    || !certificate.MatchesHostname(
                        expectedIdentity, allowWildcards: false, allowCommonName: false))
                {
                    return false;
                }

                using var root = X509Certificate2.CreateFromPem(File.ReadAllText(
                    Path.Combine(AppContext.BaseDirectory, "homewizard-ca.pem")));
                using var chain = new X509Chain();
                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(root);
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.DisableCertificateDownloads = true;
                chain.ChainPolicy.ApplicationPolicy.Add(new("1.3.6.1.5.5.7.3.1"));
                return chain.Build(certificate);
            }
        };
    }
}
