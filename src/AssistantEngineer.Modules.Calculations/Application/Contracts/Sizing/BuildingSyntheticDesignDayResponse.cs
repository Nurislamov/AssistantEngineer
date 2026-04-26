namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class BuildingSyntheticDesignDayResponse
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;

    public ReferenceDesignDayMode Mode { get; set; }

    public int DayOfYear { get; set; }
    public int Month { get; set; }

    public double DesignOutdoorDryBulbC { get; set; }
    public double OutdoorDailyRangeC { get; set; }
    public double WindSpeedMPerS { get; set; }
    public double SolarPeakWPerM2 { get; set; }

    public double CoolingSetpointC { get; set; }
    public double HeatingSetpointC { get; set; }

    public bool UseRoomSchedules { get; set; }
    public bool IncludeInternalGains { get; set; }
    public bool IncludeSolarGains { get; set; }
    public bool UseNaturalVentilation { get; set; }

    public SyntheticDesignDayScopeResponse? Building { get; set; }
    public List<SyntheticDesignDayScopeResponse> Zones { get; set; } = new();
    public List<SyntheticDesignDayScopeResponse> Rooms { get; set; } = new();
}