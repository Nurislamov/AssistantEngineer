namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

internal static class Iso52016HourlyHeatBalanceValidation
{
    public static void ThrowIfCancelled(CancellationToken cancellationToken) =>
        cancellationToken.ThrowIfCancellationRequested();
}
