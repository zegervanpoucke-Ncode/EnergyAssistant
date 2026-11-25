using System.ComponentModel;
using System.Text.Json;
using EnergyAssistant.Api.Features.HomeWizard.GetMeasurement;
using MediatR;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace EnergyAssistant.Api.Features.HomeWizard;

[McpServerToolType]
public sealed class HomeWizardTools(ISender sender)
{
    [McpServerTool(Name = "get_homewizard_measurement", ReadOnly = true, Destructive = false)]
    [Description("Read the latest HomeWizard P1 Meter measurement, including available power in watts and energy import and export in kWh.")]
    public async Task<JsonElement> GetMeasurement(CancellationToken cancellationToken)
    {
        try
        {
            return await sender.Send(new GetHomeWizardMeasurementQuery(), cancellationToken);
        }
        catch (HomeWizardReadException exception)
        {
            throw new McpException(exception.Message);
        }
    }
}
