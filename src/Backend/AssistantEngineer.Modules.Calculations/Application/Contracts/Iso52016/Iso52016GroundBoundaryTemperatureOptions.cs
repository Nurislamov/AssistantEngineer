namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016GroundBoundaryTemperatureOptions(
    Iso52016GroundBoundaryTemperatureMode Mode = Iso52016GroundBoundaryTemperatureMode.Periodic,
    double? FixedGroundTemperatureC = null,
    double? MeanAnnualGroundTemperatureC = null,
    double? AnnualGroundTemperatureAmplitudeC = null,
    double DepthM = 1.5,
    double ThermalDiffusivityM2PerDay = 0.06,
    int ColdestGroundDayOfYear = 45);

public enum Iso52016GroundBoundaryTemperatureMode
{
    OutdoorAir,
    Fixed,
    Periodic
}