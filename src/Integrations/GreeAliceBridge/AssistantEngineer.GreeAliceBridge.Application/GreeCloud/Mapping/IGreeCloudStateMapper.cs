using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.Mapping;

namespace AssistantEngineer.GreeAliceBridge.Application.GreeCloud.Mapping;

public interface IGreeCloudStateMapper
{
    GreeCloudStateMappingResult Map(GreeCloudMaskedRawStateSnapshot snapshot);
}
