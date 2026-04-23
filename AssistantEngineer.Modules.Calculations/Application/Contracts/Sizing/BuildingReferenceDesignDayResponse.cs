namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class BuildingReferenceDesignDayResponse
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public int Year { get; set; }

    public ReferenceDesignDayMode Mode { get; set; }

    public bool OccupiedHoursOnly { get; set; }
    public double OccupancyThreshold { get; set; }

    public int CoolingSeasonStartMonth { get; set; }
    public int CoolingSeasonEndMonth { get; set; }

    public int HeatingSeasonStartMonth { get; set; }
    public int HeatingSeasonEndMonth { get; set; }

    public ReferenceDesignDayScopeResponse? Building { get; set; }

    public List<ReferenceDesignDayScopeResponse> Zones { get; set; } = new();
    public List<ReferenceDesignDayScopeResponse> Rooms { get; set; } = new();
}