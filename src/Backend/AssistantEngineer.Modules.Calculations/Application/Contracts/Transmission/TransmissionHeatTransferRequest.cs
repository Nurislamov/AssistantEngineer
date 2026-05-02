namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Transmission;

public sealed record TransmissionHeatTransferRequest(
    IReadOnlyList<TransmissionElementInput> Elements);
