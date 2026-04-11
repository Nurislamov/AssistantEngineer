namespace AssistantEngineer.Models;
public class RoomCalculationResult
{
    public int RoomId { get; set; }
    public double HeatLoadWatts { get; set; }
    public double HeatLoadKilowatts { get; set; }
    public double DeltaTemperature { get; set; }
    public double BaseLoadPerSquareMeter { get; set; }
    public double HeightFactor { get; set; }
    public double TemperatureFactor { get; set; }
}