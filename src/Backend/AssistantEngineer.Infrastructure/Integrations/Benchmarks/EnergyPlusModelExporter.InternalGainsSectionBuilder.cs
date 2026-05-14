using System.Text;
using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Infrastructure.Integrations.Benchmarks;

public sealed partial class EnergyPlusModelExporter
{
    private static class EnergyPlusInternalGainsSectionBuilder
    {
        public static void Append(StringBuilder builder, Room room, string zoneName)
        {
            if (room.PeopleCount > 0)
            {
                AppendObject(
                    builder,
                    "People",
                    $"{zoneName}_People",
                    zoneName,
                    GetScheduleName(room, "Occupancy"),
                    "People",
                    F(room.PeopleCount),
                    string.Empty,
                    string.Empty,
                    "0.3",
                    "Autocalculate",
                    ActivitySchedule);
            }

            if (room.LightingLoad.Watts > 0)
            {
                AppendObject(
                    builder,
                    "Lights",
                    $"{zoneName}_Lights",
                    zoneName,
                    GetScheduleName(room, "Lighting"),
                    "LightingLevel",
                    F(room.LightingLoad.Watts),
                    string.Empty,
                    string.Empty,
                    "0",
                    "0.6",
                    "0.2",
                    "1.0",
                    "General");
            }

            if (room.EquipmentLoad.Watts > 0)
            {
                AppendObject(
                    builder,
                    "ElectricEquipment",
                    $"{zoneName}_Equipment",
                    zoneName,
                    GetScheduleName(room, "Equipment"),
                    "EquipmentLevel",
                    F(room.EquipmentLoad.Watts),
                    string.Empty,
                    string.Empty,
                    "0",
                    "0.3",
                    "0",
                    "General");
            }
        }
    }
}



