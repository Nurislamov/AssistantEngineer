namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public sealed class RoomVentilationDefaultsResponse
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public bool HasExistingVentilationParameters { get; set; }

    public bool CanApply { get; set; }
    public string Reason { get; set; } = string.Empty;

    public int DesignPeopleCount { get; set; }
    public double DesignOutdoorAirLitersPerSecond { get; set; }
    public double OutdoorAirAirChangesPerHour { get; set; }
    public double ExhaustAirChangesPerHour { get; set; }
    public double ProposedAirChangesPerHour { get; set; }

    public double HeatRecoveryEfficiency { get; set; }
    public double InfiltrationAirChangesPerHour { get; set; }
    public double WindExposureFactor { get; set; }
    public double StackCoefficient { get; set; }
    public double WindCoefficient { get; set; }

    public string SourceTableVersion { get; set; } = string.Empty;
}