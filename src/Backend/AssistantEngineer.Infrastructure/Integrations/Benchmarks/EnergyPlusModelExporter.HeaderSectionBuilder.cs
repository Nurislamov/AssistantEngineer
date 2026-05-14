using System.Text;
using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Infrastructure.Integrations.Benchmarks;

public sealed partial class EnergyPlusModelExporter
{
    private static class EnergyPlusHeaderSectionBuilder
    {
        public static void Append(StringBuilder builder, Building building)
        {
            AppendObject(builder, "Version", "24.1");
            AppendObject(builder, "SimulationControl", "Yes", "Yes", "No", "Yes", "Yes");
            AppendObject(builder, "Timestep", "4");
            AppendObject(
                builder,
                "Building",
                SafeName(building.Name),
                "0.0",
                "Suburbs",
                "0.04",
                "0.4",
                "FullExterior",
                "25",
                "6");
            AppendObject(builder, "GlobalGeometryRules", "UpperLeftCorner", "CounterClockWise", "World");
            AppendObject(builder, "RunPeriod", "Annual", "1", "1", string.Empty, "12", "31", "Tuesday", "Yes", "Yes", "No", "Yes", "Yes");

            AppendObject(builder, "ScheduleTypeLimits", "Fraction", "0", "1", "Continuous");
            AppendObject(builder, "ScheduleTypeLimits", "Temperature", "-60", "200", "Continuous", "Temperature");
            AppendObject(builder, "ScheduleTypeLimits", "Any Number");

            AppendObject(builder, "Material:NoMass", "AE_Generic_Floor_Material", "Rough", "2.0");
            AppendObject(builder, "Construction", "AE_Generic_Floor", "AE_Generic_Floor_Material");
            AppendObject(builder, "Material:NoMass", "AE_Generic_Roof_Material", "Rough", "3.0");
            AppendObject(builder, "Construction", "AE_Generic_Roof", "AE_Generic_Roof_Material");
        }
    }
}
