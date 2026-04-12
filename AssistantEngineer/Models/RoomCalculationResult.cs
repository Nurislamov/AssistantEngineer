public class RoomCalculationResult
{
    public int RoomId { get; set; }

    public double BaseRoomLoadW { get; set; }

    public double TotalWindowAreaM2 { get; set; }
    public double WindowHeatGainW { get; set; }

    public double TotalWallAreaM2 { get; set; }
    public double ExternalWallAreaM2 { get; set; }
    public double WallHeatGainW { get; set; }

    public double PeopleHeatGainW { get; set; }
    public double EquipmentHeatGainW { get; set; }
    public double LightingHeatGainW { get; set; }
    public double InternalHeatGainW { get; set; }

    public double TotalHeatLoadW { get; set; }
    public double TotalHeatLoadKw { get; set; }

    public double DeltaTemperatureC { get; set; }
    public double HeightAdjustmentFactor { get; set; }
    public double TemperatureAdjustmentFactor { get; set; }
}