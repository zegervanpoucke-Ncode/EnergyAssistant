using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

public sealed class FluviusHttpClient(
    HttpClient httpClient,
    IFluviusTokenProvider tokenProvider)
{
    public async Task<HttpResponseMessage> GetAsync(
        string relativeUrl,
        CancellationToken cancellationToken = default)
    {
        var token = await tokenProvider.GetAccessTokenAsync(
            cancellationToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            relativeUrl);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return await httpClient.SendAsync(
            request,
            cancellationToken);
    }
}