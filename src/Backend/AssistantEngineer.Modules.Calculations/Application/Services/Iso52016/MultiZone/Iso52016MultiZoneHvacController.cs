namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

internal static class Iso52016MultiZoneHvacController
{
    internal static (double[] controlledTemperatures, double[] hvacLoadsW) ApplyHvacControl(
        double[,] aMatrix,
        double[] freeFloatingTemperatures,
        double[] heatingSetpoints,
        double[] coolingSetpoints)
    {
        var zoneCount = freeFloatingTemperatures.Length;
        var responseMatrix = Iso52016MultiZoneLinearSystem.BuildResponseMatrix(aMatrix);
        var controlModes = new ControlMode[zoneCount];
        var hvacLoads = new double[zoneCount];
        var controlled = (double[])freeFloatingTemperatures.Clone();

        for (var i = 0; i < zoneCount; i++)
        {
            if (freeFloatingTemperatures[i] < heatingSetpoints[i])
                controlModes[i] = ControlMode.Heating;
            else if (freeFloatingTemperatures[i] > coolingSetpoints[i])
                controlModes[i] = ControlMode.Cooling;
        }

        for (var iteration = 0; iteration < zoneCount * 4 + 8; iteration++)
        {
            var activeIndices = Enumerable.Range(0, zoneCount)
                .Where(index => controlModes[index] != ControlMode.None)
                .ToArray();

            if (activeIndices.Length == 0)
                return (controlled, hvacLoads);

            var subMatrix = new double[activeIndices.Length, activeIndices.Length];
            var rhs = new double[activeIndices.Length];

            for (var row = 0; row < activeIndices.Length; row++)
            {
                var zoneIndex = activeIndices[row];
                var target = controlModes[zoneIndex] == ControlMode.Heating
                    ? heatingSetpoints[zoneIndex]
                    : coolingSetpoints[zoneIndex];
                rhs[row] = target - freeFloatingTemperatures[zoneIndex];

                for (var column = 0; column < activeIndices.Length; column++)
                {
                    subMatrix[row, column] = responseMatrix[zoneIndex, activeIndices[column]];
                }
            }

            var activeLoads = Iso52016MultiZoneLinearSystem.SolveLinearSystem(subMatrix, rhs);
            Array.Clear(hvacLoads, 0, hvacLoads.Length);

            for (var i = 0; i < activeIndices.Length; i++)
            {
                hvacLoads[activeIndices[i]] = activeLoads[i];
            }

            controlled = Iso52016MultiZoneLinearSystem.ApplyResponse(freeFloatingTemperatures, responseMatrix, hvacLoads);

            var changed = false;
            for (var i = 0; i < activeIndices.Length; i++)
            {
                var zoneIndex = activeIndices[i];
                if (controlModes[zoneIndex] == ControlMode.Heating && hvacLoads[zoneIndex] < 0.0)
                {
                    controlModes[zoneIndex] = ControlMode.None;
                    changed = true;
                }
                else if (controlModes[zoneIndex] == ControlMode.Cooling && hvacLoads[zoneIndex] > 0.0)
                {
                    controlModes[zoneIndex] = ControlMode.None;
                    changed = true;
                }
            }

            if (changed)
                continue;

            for (var zoneIndex = 0; zoneIndex < zoneCount; zoneIndex++)
            {
                if (controlModes[zoneIndex] != ControlMode.None)
                    continue;

                if (controlled[zoneIndex] < heatingSetpoints[zoneIndex])
                {
                    controlModes[zoneIndex] = ControlMode.Heating;
                    changed = true;
                }
                else if (controlled[zoneIndex] > coolingSetpoints[zoneIndex])
                {
                    controlModes[zoneIndex] = ControlMode.Cooling;
                    changed = true;
                }
            }

            if (!changed)
                return (controlled, hvacLoads);
        }

        return (controlled, hvacLoads);
    }

    private enum ControlMode
    {
        None = 0,
        Heating = 1,
        Cooling = 2
    }
}
