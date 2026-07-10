using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.HttpSmoke;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.HttpSmoke;

public interface IGreeAliceLocalHttpSmokePlanProvider
{
    GreeAliceLocalHttpSmokeResult GetPlan();
}
