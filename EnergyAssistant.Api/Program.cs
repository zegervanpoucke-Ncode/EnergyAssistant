using EnergyAssistant.Api.Features.HomeWizard;
using EnergyAssistant.Api.Infrastructure.HomeWizard;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddMediatR(options =>
    options.RegisterServicesFromAssemblyContaining<HomeWizardTools>());
builder.Services.AddMcpServer().WithHttpTransport().WithTools<HomeWizardTools>();
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
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "EnergyAssistant API v1"));
}

app.UseHttpsRedirection();

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

