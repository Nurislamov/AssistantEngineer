using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud;

namespace AssistantEngineer.GreeAliceBridge.Application.GreeCloud;

public interface IGreeCloudControlAdapter
{
    Task<GreeCloudControlResult> ExecuteControlAsync(
        GreeCloudControlRequest request,
        CancellationToken cancellationToken = default);
}
