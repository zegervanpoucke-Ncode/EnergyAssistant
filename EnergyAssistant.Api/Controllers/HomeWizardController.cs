using System.Text.Json;
using EnergyAssistant.Api.Features.HomeWizard;
using EnergyAssistant.Api.Features.HomeWizard.GetMeasurement;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EnergyAssistant.Api.Controllers;

[ApiController]
[Route("api/homewizard")]
public sealed class HomeWizardController(ISender sender) : ControllerBase
{
    [HttpGet("measurement")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<JsonElement>> GetMeasurement(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await sender.Send(new GetHomeWizardMeasurementQuery(), cancellationToken));
        }
        catch (HomeWizardReadException exception)
        {
            return Problem(detail: exception.Message, statusCode: exception.Failure switch
            {
                HomeWizardReadFailure.InvalidConfiguration => StatusCodes.Status503ServiceUnavailable,
                HomeWizardReadFailure.Timeout => StatusCodes.Status504GatewayTimeout,
                _ => StatusCodes.Status502BadGateway
            });
        }
    }
}

