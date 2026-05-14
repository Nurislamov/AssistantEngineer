using System.Text;
using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Infrastructure.Integrations.Benchmarks;

public sealed partial class EnergyPlusModelExporter
{
    private static class EnergyPlusScheduleSectionBuilder
    {
        public static void Append(StringBuilder builder, IReadOnlyCollection<Room> rooms)
        {
            AppendCompactSchedule(builder, AlwaysOnSchedule, "Fraction", ["Until: 24:00", "1.0"]);
            AppendCompactSchedule(builder, HeatingSetpointSchedule, "Temperature", ["Until: 24:00", "20.0"]);
            AppendCompactSchedule(builder, CoolingSetpointSchedule, "Temperature", ["Until: 24:00", "26.0"]);
            AppendCompactSchedule(builder, ActivitySchedule, "Any Number", ["Until: 24:00", "120.0"]);

            foreach (var room in rooms)
            {
                AppendHourlySchedule(builder, GetScheduleName(room, "Occupancy"), room.OccupancySchedule?.Factors);
                AppendHourlySchedule(builder, GetScheduleName(room, "Equipment"), room.EquipmentSchedule?.Factors);
                AppendHourlySchedule(builder, GetScheduleName(room, "Lighting"), room.LightingSchedule?.Factors);
            }
        }
    }
}
