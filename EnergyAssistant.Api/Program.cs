using EnergyAssistant.Api.Features.HomeWizard;
using EnergyAssistant.Api.Infrastructure.HomeWizard;
using EnergyAssistant.Api.Infrastructure.Fluvius.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddMediatR(options =>
    options.RegisterServicesFromAssemblyContaining<HomeWizardTools>());
builder.Services.AddMcpServer().WithHttpTransport().WithTools<HomeWizardTools>();
builder.Services.AddSingleton<
    IFluviusTokenProvider,
    FluviusTokenProvider>();

builder.Services.AddHttpClient<FluviusHttpClient>(client =>
{
    client.BaseAddress =
        new Uri("https://mijn.fluvius.be");
});

builder.Services.AddHttpClient<
    FluviusAuthenticationService>();

builder.Services.AddSingleton<
    IFluviusTokenStore,
    InMemoryFluviusTokenStore>();

builder.Services
    .AddOptions<FluviusOAuthOptions>()
    .Bind(builder.Configuration.GetSection(
        FluviusOAuthOptions.SectionName));

builder.Services.AddHttpClient<
    IFluviusTokenRefresher,
    FluviusOAuthTokenRefresher>();

builder.Services.AddSingleton<
    IFluviusTokenProvider,
    FluviusTokenProvider>();

builder.Services.AddHttpClient<HomeWizardClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
})
.ConfigurePrimaryHttpMessageHandler(services =>
    HomeWizardClient.CreateHandler(services.GetRequiredService<IConfiguration>()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/fluvius/test",
    async (
        FluviusHttpClient client,
        CancellationToken cancellationToken) =>
    {
        var url =
            "/verbruik/api/meter-measurement-history/" +
            "541449611000729155/report" +
            "?historyFrom=2026-10-01T00%3A00%3A00.000%2B02%3A00" +
            "&historyUntil=2026-10-20T23%3A59%3A59.999%2B02%3A00" +
            "&granularity=1" +
            "&mandateDisplayMode=1" +
            "&meterSerialNumber=1SAG1200141930";

        using var response = await client.GetAsync(
            url,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Results.StatusCode(
                (int)response.StatusCode);
        }

        var csv = await response.Content.ReadAsByteArrayAsync(
            cancellationToken);

        return Results.File(
            csv,
            "text/csv",
            "fluvius-consumption.csv");
    });


app.MapControllers();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/mcp")
        && (context.Connection.RemoteIpAddress is not { } address
            || !System.Net.IPAddress.IsLoopback(address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address)))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }

    await next(context);
});

app.MapMcp("/mcp");

app.Run();

