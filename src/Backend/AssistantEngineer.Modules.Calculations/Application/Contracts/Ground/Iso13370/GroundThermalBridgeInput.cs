namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;

public sealed record GroundThermalBridgeInput(
    bool Enabled = false,
    double LinearThermalTransmittanceWPerMK = 0.0,
    double BridgeLengthM = 0.0);

