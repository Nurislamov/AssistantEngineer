using AssistantEngineer.GreeAliceBridge.Contracts.Registry;

namespace AssistantEngineer.GreeAliceBridge.Application.Registry;

public interface IGreeAliceOfflineRegistryProvider
{
    GreeAliceRegistrySnapshot GetSnapshot();
}
