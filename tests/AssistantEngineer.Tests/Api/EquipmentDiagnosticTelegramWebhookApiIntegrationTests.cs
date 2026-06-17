using System.Net;
using System.Net.Http.Json;
using System.Text;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AssistantEngineer.Tests.Api;

public sealed class EquipmentDiagnosticTelegramWebhookApiIntegrationTests
{
    private const string Endpoint = "/api/v1/equipment-diagnostics/telegram/webhook";
    private const string SecretHeader = "X-Telegram-Bot-Api-Secret-Token";

    [Fact]
    public async Task NullAndInvalidJsonReturnBadRequest()
    {
        await using var factory = new WebhookApiFactory(enabled: true);
        var client = factory.CreateClient();

        var nullResponse = await client.PostAsync(Endpoint, new StringContent("null", Encoding.UTF8, "application/json"));
        var invalidResponse = await client.PostAsync(Endpoint, new StringContent("{", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, nullResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
    }

    [Fact]
    public async Task DisabledTransportReturnsNotFound()
    {
        await using var factory = new WebhookApiFactory(enabled: false);

        var response = await PostAsync(factory.CreateClient(), Update("/start"), "test_webhook_secret");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, factory.Outbound.CallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong_secret")]
    public async Task MissingOrWrongSecretReturnsForbidden(string? secret)
    {
        await using var factory = new WebhookApiFactory(enabled: true);

        var response = await PostAsync(factory.CreateClient(), Update("/start"), secret);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, factory.Outbound.CallCount);
    }

    [Theory]
    [InlineData("/start")]
    [InlineData("Gree H5")]
    public async Task CorrectSecretProcessesUpdateAndSendsReply(string text)
    {
        await using var factory = new WebhookApiFactory(enabled: true);

        var response = await PostAsync(factory.CreateClient(), Update(text), "test_webhook_secret");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Outbound.CallCount);
        Assert.False(string.IsNullOrWhiteSpace(factory.Outbound.LastText));
    }

    [Fact]
    public async Task UnknownChatIsAcceptedAsConsumerAndSendsReply()
    {
        await using var factory = new WebhookApiFactory(enabled: true, allowedChatId: 999);

        var response = await PostAsync(factory.CreateClient(), Update("Gree H5"), "test_webhook_secret");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, factory.Outbound.CallCount);
        Assert.Contains("Что можно сделать безопасно", factory.Outbound.LastText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeniedChatIsAcceptedAndIgnoredEvenWhenAllowed()
    {
        await using var factory = new WebhookApiFactory(enabled: true, allowedChatId: 3, deniedChatId: 3);

        var response = await PostAsync(factory.CreateClient(), Update("Gree H5"), "test_webhook_secret");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, factory.Outbound.CallCount);
    }

    [Fact]
    public async Task EnabledIdentityDiscoveryReturnsChatAndUserIdentity()
    {
        await using var factory = new WebhookApiFactory(enabled: true, enableChatIdDiscovery: true);

        var response = await PostAsync(factory.CreateClient(), Update("/whoami"), "test_webhook_secret");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("chatId: 3", factory.Outbound.LastText, StringComparison.Ordinal);
        Assert.Contains("userId: 4", factory.Outbound.LastText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResponseNeverExposesSecretsOrInternalPaths()
    {
        await using var factory = new WebhookApiFactory(enabled: true);

        var response = await PostAsync(factory.CreateClient(), Update("Gree H5"), "wrong_secret");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("test_webhook_secret", body, StringComparison.Ordinal);
        Assert.DoesNotContain("test-token-value", body, StringComparison.Ordinal);
        Assert.DoesNotContain("artifacts/verification", body, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        TelegramWebhookUpdateDto update,
        string? secret)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(update)
        };
        if (secret is not null)
        {
            request.Headers.Add(SecretHeader, secret);
        }

        return await client.SendAsync(request);
    }

    private static TelegramWebhookUpdateDto Update(string text) =>
        new(1, new TelegramWebhookMessageDto(2, text, new TelegramWebhookChatDto(3, "operator"), new TelegramWebhookUserDto(4, "operator"), 1_700_000_000));

    private sealed class WebhookApiFactory(
        bool enabled,
        long? allowedChatId = 3,
        long? deniedChatId = null,
        bool enableChatIdDiscovery = false) : WebApplicationFactory<Program>
    {
        public FakeOutbound Outbound { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(configuration =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=AssistantEngineerTests",
                    ["EngineeringWorkflowPersistence:Provider"] = "InMemory",
                    ["EnergyPlus:UseDocker"] = "false",
                    ["Authentication:ApiKey:Enabled"] = "false",
                    ["ApiHardening:RateLimiting:Enabled"] = "false",
                    ["AssistantEngineer:EquipmentDiagnostics:Telegram:IsEnabled"] = enabled.ToString(),
                    ["AssistantEngineer:EquipmentDiagnostics:Telegram:WebhookSecret"] = "test_webhook_secret",
                    ["AssistantEngineer:EquipmentDiagnostics:Telegram:BotToken"] = "test-token-value",
                    ["AssistantEngineer:EquipmentDiagnostics:Telegram:AllowedChatIds:0"] = allowedChatId?.ToString(),
                    ["AssistantEngineer:EquipmentDiagnostics:Telegram:DeniedChatIds:0"] = deniedChatId?.ToString(),
                    ["AssistantEngineer:EquipmentDiagnostics:Telegram:EnableChatIdDiscovery"] = enableChatIdDiscovery.ToString()
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEquipmentDiagnosticTelegramOutboundClient>();
                services.RemoveAll<ITelegramUserStore>();
                services.RemoveAll<EquipmentDiagnosticTelegramWebhookOptions>();
                services.RemoveAll<EquipmentDiagnosticTelegramOptions>();
                var chatIds = allowedChatId is null ? Array.Empty<long>() : [allowedChatId.Value];
                var deniedChatIds = deniedChatId is null ? Array.Empty<long>() : [deniedChatId.Value];
                services.AddSingleton(new EquipmentDiagnosticTelegramWebhookOptions
                {
                    IsEnabled = enabled,
                    WebhookSecret = "test_webhook_secret",
                    BotToken = "test-token-value",
                    AllowedChatIds = chatIds,
                    DeniedChatIds = deniedChatIds,
                    EnableChatIdDiscovery = enableChatIdDiscovery
                });
                services.AddSingleton(new EquipmentDiagnosticTelegramOptions
                {
                    IsEnabled = enabled,
                    AllowedChatIds = chatIds,
                    DeniedChatIds = deniedChatIds,
                    EnableChatIdDiscovery = enableChatIdDiscovery,
                    DefaultManufacturer = "Gree"
                });
                services.AddSingleton<ITelegramUserStore, InMemoryTelegramUserStore>();
                services.AddSingleton<IEquipmentDiagnosticTelegramOutboundClient>(Outbound);
            });
        }
    }

    private sealed class FakeOutbound : IEquipmentDiagnosticTelegramOutboundClient
    {
        public int CallCount { get; private set; }
        public string? LastText { get; private set; }

        public Task<EquipmentDiagnosticTelegramOutboundResult> SendMessageAsync(
            long chatId,
            string text,
            string? parseMode,
            bool disableWebPagePreview,
            EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastText = text;
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(true, "Sent."));
        }
    }
}
