using System.Text;

namespace AssistantEngineer.Infrastructure.Integrations.Benchmarks;

public sealed partial class EnergyPlusModelExporter
{
    private static class EnergyPlusOutputSectionBuilder
    {
        public static void Append(StringBuilder builder)
        {
            AppendObject(builder, "Output:Variable", "*", "Zone Ideal Loads Supply Air Total Cooling Energy", "Hourly");
            AppendObject(builder, "Output:Variable", "*", "Zone Ideal Loads Supply Air Total Heating Energy", "Hourly");
            AppendObject(builder, "Output:Meter", "Electricity:Facility", "Hourly");
            AppendObject(builder, "Output:Meter", "Heating:EnergyTransfer", "Hourly");
            AppendObject(builder, "Output:Meter", "Cooling:EnergyTransfer", "Hourly");
        }
    }
}




