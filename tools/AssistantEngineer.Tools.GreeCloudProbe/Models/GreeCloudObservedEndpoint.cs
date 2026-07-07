namespace AssistantEngineer.Tools.GreeCloudProbe.Models;

internal sealed record GreeCloudObservedEndpoint(
    string Name,
    string Host,
    int Port,
    string Protocol,
    string Purpose,
    string Source,
    bool RequiredForCloudBridge);
