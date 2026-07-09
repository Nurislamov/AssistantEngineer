namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Vrf;

public static class GreeAliceVrfYandexExposurePolicy
{
    public static bool ShouldExpose(GreeAliceVrfGateway gateway)
    {
        ArgumentNullException.ThrowIfNull(gateway);

        return gateway.ExposeToYandex && !gateway.IsTechnicalDevice;
    }

    public static bool ShouldExpose(GreeAliceVrfChildUnit childUnit)
    {
        ArgumentNullException.ThrowIfNull(childUnit);

        return childUnit.ExposeToYandex
            && string.Equals(childUnit.DeviceKind, "vrf-child-indoor-unit", StringComparison.Ordinal);
    }
}
