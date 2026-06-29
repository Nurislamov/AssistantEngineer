using AssistantEngineer.Infrastructure.Persistence;
using AssistantEngineer.Infrastructure.Persistence.Repositories;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests;

public sealed class EfTelegramUserStoreTests
{
    [Fact]
    public async Task EfTelegramUserStorePersistsConsumerIdentityAfterProviderRecreation()
    {
        await using var connection = await OpenConnectionAsync();
        await using (var provider = await BuildProviderAsync(connection))
        {
            var store = provider.GetRequiredService<ITelegramUserStore>();

            await store.GetOrCreateConsumerAsync(NewUpdate(
                chatId: 101,
                userId: 5_001,
                username: "consumer_user",
                firstName: "Test",
                lastName: "Consumer",
                receivedAt: new DateTimeOffset(2026, 6, 17, 10, 15, 0, TimeSpan.Zero)));
        }

        await using (var provider = await BuildProviderAsync(connection))
        {
            var store = provider.GetRequiredService<ITelegramUserStore>();
            var user = await store.GetByChatIdAsync(101);

            Assert.NotNull(user);
            Assert.Equal(5_001, user.TelegramUserId);
            Assert.Equal("consumer_user", user.Username);
            Assert.Equal("Test", user.FirstName);
            Assert.Equal("Consumer", user.LastName);
            Assert.Equal(TelegramUserRole.Consumer, user.Role);
            Assert.True(user.IsEnabled);
            Assert.False(user.IsBlocked);
            Assert.Equal(new DateTimeOffset(2026, 6, 17, 10, 15, 0, TimeSpan.Zero), user.LastSeenAt);
        }
    }

    [Fact]
    public async Task EfTelegramUserStorePersistsRoleFlagsPhoneAndAccessTimestamps()
    {
        await using var connection = await OpenConnectionAsync();
        await using (var provider = await BuildProviderAsync(connection))
        {
            var store = provider.GetRequiredService<ITelegramUserStore>();

            await store.GetOrCreateConsumerAsync(NewUpdate(chatId: 202, userId: 5_202));
            await store.SetRoleAsync(202, TelegramUserRole.Engineer);
            await store.SetEnabledAsync(202, isEnabled: false);
            await store.SetBlockedAsync(202, isBlocked: true);
            await store.SavePhoneAsync(
                202,
                "+10000000000",
                verified: true,
                TelegramUserPhoneNumberSource.TelegramContact,
                new DateTimeOffset(2026, 6, 17, 11, 0, 0, TimeSpan.Zero));
            await store.MarkAccessDeniedAsync(202);
        }

        await using (var provider = await BuildProviderAsync(connection))
        {
            var store = provider.GetRequiredService<ITelegramUserStore>();
            var user = await store.GetByChatIdAsync(202);

            Assert.NotNull(user);
            Assert.Equal(TelegramUserRole.Engineer, user.Role);
            Assert.False(user.IsEnabled);
            Assert.True(user.IsBlocked);
            Assert.True(user.HasPhoneNumber);
            Assert.True(user.PhoneNumberVerified);
            Assert.Equal(TelegramUserPhoneNumberSource.TelegramContact, user.PhoneNumberSource);
            Assert.NotNull(user.LastAccessDeniedAt);
        }
    }

    [Fact]
    public async Task EfTelegramUserStorePersistsBootstrapOwnerAndGetOrCreateDoesNotDowngrade()
    {
        await using var connection = await OpenConnectionAsync();
        await using (var provider = await BuildProviderAsync(connection))
        {
            var store = provider.GetRequiredService<ITelegramUserStore>();

            await store.EnsureBootstrapOwnerAsync(NewUpdate(chatId: 303, userId: null));
        }

        await using (var provider = await BuildProviderAsync(connection))
        {
            var store = provider.GetRequiredService<ITelegramUserStore>();

            var owner = await store.GetOrCreateConsumerAsync(NewUpdate(
                chatId: 303,
                userId: 5_303,
                username: "owner_user",
                firstName: "Owner",
                lastName: "Persisted"));

            Assert.Equal(TelegramUserRole.Owner, owner.Role);
            Assert.True(owner.IsEnabled);
            Assert.False(owner.IsBlocked);
            Assert.Equal(5_303, owner.TelegramUserId);
            Assert.Equal("owner_user", owner.Username);
        }
    }

    [Theory]
    [InlineData(TelegramUserRole.Owner)]
    [InlineData(TelegramUserRole.Admin)]
    [InlineData(TelegramUserRole.Engineer)]
    [InlineData(TelegramUserRole.Installer)]
    public async Task EfTelegramUserStoreGetOrCreateConsumerDoesNotDowngradeExistingPrivilegedRoles(
        TelegramUserRole role)
    {
        await using var connection = await OpenConnectionAsync();
        await using var provider = await BuildProviderAsync(connection);
        var store = provider.GetRequiredService<ITelegramUserStore>();

        await store.AllowAsync(404, role);

        var user = await store.GetOrCreateConsumerAsync(NewUpdate(chatId: 404, userId: 5_404));

        Assert.Equal(role, user.Role);
        Assert.Equal(5_404, user.TelegramUserId);
    }

    [Fact]
    public async Task EfTelegramUserStoreTelegramUserIdLookupPrefersCanonicalManagerDuplicate()
    {
        await using var connection = await OpenConnectionAsync();
        await using var provider = await BuildProviderAsync(connection);

        await SeedUserAsync(
            provider,
            chatId: 505,
            telegramUserId: 7_777,
            TelegramUserRole.Consumer);
        await SeedUserAsync(
            provider,
            chatId: 606,
            telegramUserId: 7_777,
            TelegramUserRole.Admin);

        var store = provider.GetRequiredService<ITelegramUserStore>();
        var user = await store.GetByTelegramUserIdAsync(7_777);

        Assert.NotNull(user);
        Assert.Equal(606, user.TelegramChatId);
        Assert.Equal(TelegramUserRole.Admin, user.Role);
    }

    [Fact]
    public async Task EfTelegramUserStoreTelegramUserIdLookupIgnoresBlockedManagerBeforeActiveAdmin()
    {
        await using var connection = await OpenConnectionAsync();
        await using var provider = await BuildProviderAsync(connection);

        await SeedUserAsync(
            provider,
            chatId: 707,
            telegramUserId: 8_888,
            TelegramUserRole.Owner,
            isEnabled: true,
            isBlocked: true);
        await SeedUserAsync(
            provider,
            chatId: 808,
            telegramUserId: 8_888,
            TelegramUserRole.Admin);

        var store = provider.GetRequiredService<ITelegramUserStore>();
        var user = await store.GetByTelegramUserIdAsync(8_888);

        Assert.NotNull(user);
        Assert.Equal(808, user.TelegramChatId);
        Assert.Equal(TelegramUserRole.Admin, user.Role);
    }

    [Fact]
    public async Task EfTelegramUserStoreModelAndMigrationsContainTelegramUserPersistentState()
    {
        await using var connection = await OpenConnectionAsync();
        await using var provider = await BuildProviderAsync(connection);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = context.Model.FindEntityType(typeof(TelegramUserEntity));

        Assert.NotNull(entityType);
        Assert.Equal("TelegramUsers", entityType.GetTableName());
        Assert.NotNull(entityType.FindProperty(nameof(TelegramUserEntity.Role)));
        Assert.NotNull(entityType.FindProperty(nameof(TelegramUserEntity.IsEnabled)));
        Assert.NotNull(entityType.FindProperty(nameof(TelegramUserEntity.IsBlocked)));
        Assert.NotNull(entityType.FindProperty(nameof(TelegramUserEntity.PhoneNumber)));
        Assert.NotNull(entityType.FindProperty(nameof(TelegramUserEntity.PhoneNumberSource)));
        Assert.NotNull(entityType.FindProperty(nameof(TelegramUserEntity.LastSeenAt)));
        Assert.NotNull(entityType.FindProperty(nameof(TelegramUserEntity.LastAccessDeniedAt)));
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.Properties.Single().Name == nameof(TelegramUserEntity.TelegramUserId));

        Assert.True(File.Exists(Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Infrastructure",
            "Persistence",
            "Migrations",
            "20260617062738_AddTelegramUsers.cs")));
        Assert.True(File.Exists(Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Infrastructure",
            "Persistence",
            "Migrations",
            "20260617120000_AddTelegramUserPhoneSource.cs")));
    }

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
        services.AddSingleton<ITelegramUserStore, EfTelegramUserStore>();

        var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
        return provider;
    }

    private static EquipmentDiagnosticTelegramUpdate NewUpdate(
        long chatId,
        long? userId,
        string? username = null,
        string? firstName = null,
        string? lastName = null,
        DateTimeOffset? receivedAt = null) =>
        new(
            UpdateId: chatId,
            ChatId: chatId,
            Username: username,
            Text: "/start",
            ReceivedAt: receivedAt ?? new DateTimeOffset(2026, 6, 17, 9, 0, 0, TimeSpan.Zero),
            UserId: userId,
            FirstName: firstName,
            LastName: lastName);

    private static async Task SeedUserAsync(
        ServiceProvider provider,
        long chatId,
        long telegramUserId,
        TelegramUserRole role,
        bool isEnabled = true,
        bool isBlocked = false)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.TelegramUsers.Add(new TelegramUserEntity
        {
            TelegramChatId = chatId,
            TelegramUserId = telegramUserId,
            Username = $"user_{chatId}",
            Role = role,
            IsEnabled = isEnabled,
            IsBlocked = isBlocked,
            CreatedAt = new DateTimeOffset(2026, 6, 17, 9, 30, 0, TimeSpan.Zero),
            LastSeenAt = new DateTimeOffset(2026, 6, 17, 9, 30, 0, TimeSpan.Zero)
        });
        await context.SaveChangesAsync();
    }
}
