using System.Text;

namespace AssistantEngineer.Infrastructure.Integrations.Benchmarks;

public sealed partial class EnergyPlusModelExporter
{
    private static class EnergyPlusIdealLoadsSectionBuilder
    {
        public static void Append(StringBuilder builder, string zoneName)
        {
            AppendObject(
                builder,
                "ZoneControl:Thermostat",
                $"{zoneName}_Thermostat",
                zoneName,
                AlwaysOnSchedule,
                "ThermostatSetpoint:DualSetpoint",
                $"{zoneName}_Dual_Setpoint");

            AppendObject(
                builder,
                "ThermostatSetpoint:DualSetpoint",
                $"{zoneName}_Dual_Setpoint",
                HeatingSetpointSchedule,
                CoolingSetpointSchedule);

            AppendObject(
                builder,
                "Sizing:Zone",
                zoneName,
                "SupplyAirTemperature",
                "14",
                "SupplyAirTemperature",
                "50",
                "0.008",
                "0.008",
                "DesignDay",
                "0",
                "0",
                "DesignDay",
                "0",
                "0");

            AppendObject(
                builder,
                "ZoneHVAC:EquipmentConnections",
                zoneName,
                $"{zoneName}_Equipment",
                $"{zoneName}_Inlets",
                string.Empty,
                $"{zoneName}_Air_Node",
                $"{zoneName}_Return_Node");

            AppendObject(
                builder,
                "ZoneHVAC:EquipmentList",
                $"{zoneName}_Equipment",
                "SequentialLoad",
                "ZoneHVAC:IdealLoadsAirSystem",
                $"{zoneName}_Ideal_Loads",
                "1",
                "1",
                "1",
                "1");

            AppendObject(builder, "NodeList", $"{zoneName}_Inlets", $"{zoneName}_Supply_Inlet");

            AppendObject(
                builder,
                "ZoneHVAC:IdealLoadsAirSystem",
                $"{zoneName}_Ideal_Loads",
                AlwaysOnSchedule,
                $"{zoneName}_Supply_Inlet",
                string.Empty,
                string.Empty,
                "50",
                "13",
                "0.0156",
                "0.0077",
                "NoLimit",
                string.Empty,
                string.Empty,
                string.Empty,
                "NoLimit",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty);
        }
    }
}




