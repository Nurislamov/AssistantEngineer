namespace AssistantEngineer.Models;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    public double Area { get; set; }
    public double Volume { get; set; }
    
    public double Height { get; set; }
    
    public double IndoorTemperature { get; set; }
    public double OutdoorTemperature { get; set; }
}