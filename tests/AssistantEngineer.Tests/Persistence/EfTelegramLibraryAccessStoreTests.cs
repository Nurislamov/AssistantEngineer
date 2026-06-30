using AssistantEngineer.Infrastructure.Persistence;
using AssistantEngineer.Infrastructure.Persistence.Repositories;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests;

public sealed class EfTelegramLibraryAccessStoreTests
{
    [Fact]
    public async Task EfTelegramLibraryAccessStorePersistsGrantsAndRequests()
    {
        await using var connection = await OpenConnectionAsync();
        TelegramUserSnapshot user;
        TelegramLibraryAccessRequest request;
        await using (var provider = await BuildProviderAsync(connection))
        {
            user = await SeedUserAsync(provider, 700, TelegramUserRole.Engineer);
            var store = provider.GetRequiredService<ITelegramLibraryAccessStore>();

            request = await store.CreateOrGetPendingRequestAsync(user);
            var duplicate = await store.CreateOrGetPendingRequestAsync(user);

            Assert.Equal(request.Id, duplicate.Id);
            Assert.False(await store.HasActiveGrantAsync(user.Id));

            await store.ResolveRequestAsync(
                request.Id,
                TelegramLibraryAccessRequestStatus.Approved,
                resolvedByTelegramUserDatabaseId: user.Id);
            await store.GrantAsync(user.Id, grantedByTelegramUserDatabaseId: user.Id, "test");
        }

        await using (var provider = await BuildProviderAsync(connection))
        {
            var store = provider.GetRequiredService<ITelegramLibraryAccessStore>();
            var pending = await store.ListPendingRequestsAsync(10);
            var resolved = await store.GetRequestAsync(request.Id);
            var grant = await store.GetActiveGrantAsync(user.Id);

            Assert.Empty(pending);
            Assert.NotNull(resolved);
            Assert.Equal(TelegramLibraryAccessRequestStatus.Approved, resolved.Status);
            Assert.NotNull(grant);
            Assert.True(await store.HasActiveGrantAsync(user.Id));
        }
    }

    [Fact]
    public async Task EfTelegramLibraryAccessStorePersistsExplicitOwnerRequestedRole()
    {
        await using var connection = await OpenConnectionAsync();
        await using var provider = await BuildProviderAsync(connection);
        var user = await SeedUserAsync(provider, 701, TelegramUserRole.Owner);
        var store = provider.GetRequiredService<ITelegramLibraryAccessStore>();

        var request = await store.CreateOrGetPendingRequestAsync(user);
        var persisted = await store.GetRequestAsync(request.Id);

        Assert.Equal(TelegramUserRole.Owner, request.RequestedRole);
        Assert.Equal(TelegramUserRole.Owner, persisted?.RequestedRole);
    }

    [Fact]
    public async Task EfTelegramLibraryAccessModelAndMigrationContainPersistentState()
    {
        await using var connection = await OpenConnectionAsync();
        await using var provider = await BuildProviderAsync(connection);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var grantType = context.Model.FindEntityType(typeof(TelegramLibraryAccessGrantEntity));
        var requestType = context.Model.FindEntityType(typeof(TelegramLibraryAccessRequestEntity));

        Assert.NotNull(grantType);
        Assert.Equal("TelegramLibraryAccessGrants", grantType.GetTableName());
        Assert.NotNull(grantType.FindProperty(nameof(TelegramLibraryAccessGrantEntity.TelegramUserId)));
        Assert.NotNull(grantType.FindProperty(nameof(TelegramLibraryAccessGrantEntity.GrantedByTelegramUserId)));
        Assert.NotNull(grantType.FindProperty(nameof(TelegramLibraryAccessGrantEntity.RevokedAt)));

        Assert.NotNull(requestType);
        Assert.Equal("TelegramLibraryAccessRequests", requestType.GetTableName());
        var requestedRoleProperty = requestType.FindProperty(nameof(TelegramLibraryAccessRequestEntity.RequestedRole));
        Assert.NotNull(requestedRoleProperty);
        Assert.Equal((TelegramUserRole)(-1), requestedRoleProperty.Sentinel);
        Assert.NotNull(requestType.FindProperty(nameof(TelegramLibraryAccessRequestEntity.Status)));
        Assert.NotNull(requestType.FindProperty(nameof(TelegramLibraryAccessRequestEntity.ResolvedByTelegramUserId)));

        var migrationsDirectory = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Infrastructure",
            "Persistence",
            "Migrations");
        Assert.Contains(
            Directory.EnumerateFiles(migrationsDirectory, "*AddTelegramFileLibrary.cs"),
            path => path.EndsWith("AddTelegramFileLibrary.cs", StringComparison.Ordinal));
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
        services.AddSingleton<ITelegramLibraryAccessStore, EfTelegramLibraryAccessStore>();

        var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
        return provider;
    }

    private static async Task<TelegramUserSnapshot> SeedUserAsync(
        ServiceProvider provider,
        long chatId,
        TelegramUserRole role)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = new TelegramUserEntity
        {
            TelegramChatId = chatId,
            TelegramUserId = chatId + 1000,
            Username = $"user_{chatId}",
            Role = role,
            IsEnabled = true,
            IsBlocked = false,
            CreatedAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow
        };
        context.TelegramUsers.Add(entity);
        await context.SaveChangesAsync();
        return new TelegramUserSnapshot(
            entity.Id,
            entity.TelegramChatId,
            entity.TelegramUserId,
            entity.Username,
            entity.FirstName,
            entity.LastName,
            entity.Role,
            entity.IsEnabled,
            entity.IsBlocked,
            entity.PhoneNumberVerified,
            false,
            entity.PhoneNumberSource,
            entity.CreatedAt,
            entity.LastSeenAt,
            entity.LastAccessDeniedAt);
    }
}
