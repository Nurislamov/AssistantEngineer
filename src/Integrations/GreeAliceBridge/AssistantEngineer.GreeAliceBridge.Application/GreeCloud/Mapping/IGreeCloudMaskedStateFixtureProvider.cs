using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.Mapping;

namespace AssistantEngineer.GreeAliceBridge.Application.GreeCloud.Mapping;

public interface IGreeCloudMaskedStateFixtureProvider
{
    GreeCloudMaskedRawStateSnapshot GetSnapshot(string deviceId);
}
