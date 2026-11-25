namespace EnergyAssistant.Api.Features.HomeWizard;

internal enum HomeWizardReadFailure
{
    InvalidConfiguration,
    Timeout,
    MeterError
}

internal sealed class HomeWizardReadException(
    HomeWizardReadFailure failure, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public HomeWizardReadFailure Failure { get; } = failure;
}
