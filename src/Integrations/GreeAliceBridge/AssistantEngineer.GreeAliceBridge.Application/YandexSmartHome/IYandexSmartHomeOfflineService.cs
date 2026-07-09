using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome;

public interface IYandexSmartHomeOfflineService
{
    YandexDevicesResponse GetDevices();

    YandexQueryResponse QueryDevices(YandexQueryRequest request);

    YandexActionResponse ExecuteAction(YandexActionRequest request);

    YandexUnlinkResponse Unlink(string userId);
}
