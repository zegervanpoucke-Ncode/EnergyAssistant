using System.Text.Json;
using MediatR;
using EnergyAssistant.Api.Infrastructure.HomeWizard;

namespace EnergyAssistant.Api.Features.HomeWizard.GetMeasurement;

public sealed record GetHomeWizardMeasurementQuery : IRequest<JsonElement>;

internal sealed class GetHomeWizardMeasurementHandler(HomeWizardClient client)
    : IRequestHandler<GetHomeWizardMeasurementQuery, JsonElement>
{
    public Task<JsonElement> Handle(
        GetHomeWizardMeasurementQuery request, CancellationToken cancellationToken) =>
        client.GetMeasurementAsync(cancellationToken);
}
