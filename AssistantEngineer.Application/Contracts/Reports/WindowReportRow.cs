namespace AssistantEngineer.Contracts.Reports;

public class WindowReportRow
{
    public int WindowId { get; set; }
    public int RoomId { get; set; }

    public string FloorName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;

    public double AreaM2 { get; set; }
}