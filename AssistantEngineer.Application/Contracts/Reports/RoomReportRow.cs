namespace AssistantEngineer.Contracts.Reports;

public class RoomReportRow
{
    public int RoomId { get; set; }

    public string ProjectName { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public string FloorName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;

    public double AreaM2 { get; set; }
    public double HeightM { get; set; }
    public double VolumeM3 { get; set; }

    public double IndoorTemperatureC { get; set; }
    public double OutdoorTemperatureC { get; set; }

    public int PeopleCount { get; set; }
    public double EquipmentLoadW { get; set; }
    public double LightingLoadW { get; set; }

    public double TotalWindowAreaM2 { get; set; }
    public double TotalWallAreaM2 { get; set; }
    public double ExternalWallAreaM2 { get; set; }

    public double BaseRoomLoadW { get; set; }
    public double WindowHeatGainW { get; set; }
    public double WallHeatGainW { get; set; }
    public double InternalHeatGainW { get; set; }

    public double DesignReserveFactor { get; set; }
    public double DesignCapacityW { get; set; }
    public double DesignCapacityKw { get; set; }

    public double TotalHeatLoadW { get; set; }
    public double TotalHeatLoadKw { get; set; }

    public string RequestedSystemType { get; set; } = string.Empty;
    public string RequestedUnitType { get; set; } = string.Empty;

    public int? SelectedCatalogItemId { get; set; }
    public string SelectedManufacturer { get; set; } = string.Empty;
    public string SelectedModelName { get; set; } = string.Empty;
    public double? SelectedNominalCoolingCapacityKw { get; set; }

    public double? SelectionReserveKw { get; set; }
    public bool EquipmentSelected { get; set; }
}
