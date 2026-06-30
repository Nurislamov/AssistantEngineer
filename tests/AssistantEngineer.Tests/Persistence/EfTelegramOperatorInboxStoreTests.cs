using AssistantEngineer.Infrastructure.Persistence;
using AssistantEngineer.Infrastructure.Persistence.Repositories;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.OperatorInbox;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests;

public sealed class EfTelegramOperatorInboxStoreTests
{
    [Fact]
    public async Task EfTelegramOperatorInboxStorePersistsThreadMessagesAndReplyLink()
    {
        await using var connection = await OpenConnectionAsync();
        await using var provider = await BuildProviderAsync(connection);
        var store = provider.GetRequiredService<ITelegramOperatorInboxStore>();
        var access = Access();

        var userMessage = await store.AddUserMessageAsync(
            access,
            Update("Нужна помощь"),
            TelegramOperatorInboxMessageKind.Text,
            "Нужна помощь");
        await store.SetOperatorMessageAsync(userMessage.Message.Id, -100500, 701);
        var linked = await store.GetByOperatorMessageAsync(-100500, 701);
        var reply = await store.AddOperatorReplyAsync(
            userMessage.Thread.Id,
            userChatId: 20,
            operatorChatId: -100500,
            operatorMessageId: 702,
            operatorReplyToMessageId: 701,
            text: "Ответ");
        var thread = await store.GetThreadAsync(userMessage.Thread.Id);

        Assert.NotNull(linked);
        Assert.Equal(userMessage.Thread.Id, linked.ThreadId);
        Assert.Equal(TelegramOperatorInboxMessageDirection.OperatorToUser, reply.Direction);
        Assert.Equal(701, reply.OperatorReplyToMessageId);
        Assert.NotNull(thread);
        Assert.Equal(TelegramOperatorInboxThreadStatus.Answered, thread.Status);
        Assert.NotNull(thread.LastOwnerReplyAt);
    }

    [Fact]
    public async Task EfTelegramOperatorInboxStorePersistsVideoNoteMessageKindWithoutNewSchema()
    {
        await using var connection = await OpenConnectionAsync();
        await using var provider = await BuildProviderAsync(connection);
        var store = provider.GetRequiredService<ITelegramOperatorInboxStore>();

        var userMessage = await store.AddUserMessageAsync(
            Access(),
            Update(text: null) with { HasVideoNote = true },
            TelegramOperatorInboxMessageKind.VideoNote,
            text: null);
        await store.SetOperatorMessageAsync(userMessage.Message.Id, -100500, 703);
        var linked = await store.GetByOperatorMessageAsync(-100500, 703);

        Assert.NotNull(linked);
        Assert.Equal(TelegramOperatorInboxMessageKind.VideoNote, linked.MessageKind);
    }

    [Fact]
    public async Task EfTelegramOperatorInboxModelAndMigrationContainPersistentState()
    {
        await using var connection = await OpenConnectionAsync();
        await using var provider = await BuildProviderAsync(connection);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var threadType = context.Model.FindEntityType(typeof(TelegramOperatorInboxThreadEntity));
        var messageType = context.Model.FindEntityType(typeof(TelegramOperatorInboxMessageEntity));

        Assert.NotNull(threadType);
        Assert.NotNull(messageType);
        Assert.Equal("TelegramOperatorInboxThreads", threadType.GetTableName());
        Assert.Equal("TelegramOperatorInboxMessages", messageType.GetTableName());
        Assert.NotNull(messageType.FindProperty(nameof(TelegramOperatorInboxMessageEntity.OperatorReplyToMessageId)));
        Assert.Contains(
            messageType.GetIndexes(),
            index => index.Properties.Select(property => property.Name).SequenceEqual(
                [
                    nameof(TelegramOperatorInboxMessageEntity.OperatorChatId),
                    nameof(TelegramOperatorInboxMessageEntity.OperatorMessageId)
                ]));
        Assert.Contains(
            messageType.GetIndexes(),
            index => index.Properties.Select(property => property.Name).SequenceEqual(
                [
                    nameof(TelegramOperatorInboxMessageEntity.UserChatId),
                    nameof(TelegramOperatorInboxMessageEntity.UserMessageId)
                ]));

        var migrationsDirectory = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Infrastructure",
            "Persistence",
            "Migrations");
        Assert.Contains(
            Directory.EnumerateFiles(migrationsDirectory, "*AddTelegramOperatorInbox.cs"),
            path => path.EndsWith("AddTelegramOperatorInbox.cs", StringComparison.Ordinal));
    }

    private static TelegramUserAccessResult Access()
    {
        var user = new TelegramUserSnapshot(
            Id: 5,
            TelegramChatId: 20,
            TelegramUserId: 200,
            Username: "customer",
            FirstName: "Иван",
            LastName: null,
            Role: TelegramUserRole.Consumer,
            IsEnabled: true,
            IsBlocked: false,
            PhoneNumberVerified: false,
            HasPhoneNumber: false,
            PhoneNumberSource: null,
            CreatedAt: DateTimeOffset.UtcNow,
            LastSeenAt: DateTimeOffset.UtcNow,
            LastAccessDeniedAt: null);
        return new TelegramUserAccessResult(true, user, TelegramUserRole.Consumer);
    }

    private static EquipmentDiagnosticTelegramUpdate Update(string? text) =>
        new(
            UpdateId: 1,
            ChatId: 20,
            Username: "customer",
            Text: text,
            MessageId: 55,
            UserId: 200,
            FirstName: "Иван",
            ChatType: "private");

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:;Cache=Shared");
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<ServiceProvider> BuildProviderAsync(SqliteConnection connection)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
        services.AddSingleton<ITelegramOperatorInboxStore, EfTelegramOperatorInboxStore>();

        var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
        return provider;
    }
}
