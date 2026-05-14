using System.Text;
using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Infrastructure.Integrations.Benchmarks;

public sealed partial class EnergyPlusModelExporter
{
    private static class EnergyPlusVentilationInfiltrationSectionBuilder
    {
        public static void Append(StringBuilder builder, Room room, string zoneName)
        {
            var airChangesPerHour = room.VentilationParameters?.AirChangesPerHour ?? 0;
            if (airChangesPerHour <= 0)
                return;

            AppendObject(
                builder,
                "ZoneInfiltration:DesignFlowRate",
                $"{zoneName}_Ventilation",
                zoneName,
                AlwaysOnSchedule,
                "AirChanges/Hour",
                string.Empty,
                string.Empty,
                string.Empty,
                F(airChangesPerHour),
                "1",
                "0",
                "0",
                "0");
        }
    }
}



