namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Vrf;

public static class GreeAliceVrfChildUnitStableId
{
    public static string Resolve(GreeAliceVrfChildUnit childUnit)
    {
        ArgumentNullException.ThrowIfNull(childUnit);

        return childUnit.StableYandexDeviceId;
    }
}
