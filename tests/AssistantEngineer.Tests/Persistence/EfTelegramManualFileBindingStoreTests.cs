using AssistantEngineer.Infrastructure.Persistence;
using AssistantEngineer.Infrastructure.Persistence.Repositories;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests;

public sealed class EfTelegramManualFileBindingStoreTests
{
    [Fact]
    public async Task EfTelegramManualFileBindingStorePersistsSeriesBindingAfterProviderRecreation()
    {
        await using var connection = await OpenConnectionAsync();
        await using (var provider = await BuildProviderAsync(connection))
        {
            var store = provider.GetRequiredService<ITelegramManualFileBindingStore>();

            await store.UpsertSeriesAsync(new TelegramManualFileBinding(
                "gree-gmv9-flex-service-manual",
                "telegram-file-id-gmv9-flex",
                "Gree GMV9 Flex Service Manual EN Rev B.pdf",
                "application/pdf",
                new DateTimeOffset(2026, 6, 29, 10, 0, 0, TimeSpan.Zero),
                "TelegramManualBind",
                "Owner",
                "telegram-unique-gmv9-flex",
                123_456,
                "Gree",
                "GMV9 Flex",
                7_777,
                101));
        }

        await using (var provider = await BuildProviderAsync(connection))
        {
            var store = provider.GetRequiredService<ITelegramManualFileBindingStore>();

            var binding = await store.GetBySeriesAsync("gree", "gmv9 flex");

            Assert.NotNull(binding);
            Assert.Equal("telegram-file-id-gmv9-flex", binding.TelegramFileId);
            Assert.Equal("telegram-unique-gmv9-flex", binding.TelegramFileUniqueId);
            Assert.Equal("Gree GMV9 Flex Service Manual EN Rev B.pdf", binding.OriginalFileName);
            Assert.Equal("application/pdf", binding.ContentType);
            Assert.Equal(123_456, binding.FileSize);
            Assert.Equal("Gree", binding.Brand);
            Assert.Equal("GMV9 Flex", binding.Series);
            Assert.Equal(7_777, binding.UploadedByTelegramUserId);
            Assert.Equal(101, binding.UploadedByTelegramChatId);
            Assert.Equal("Owner", binding.RegisteredByRole);
            Assert.True(binding.IsActive);
            Assert.Equal(TelegramLibraryDocumentType.ServiceManual, binding.DocumentType);
            Assert.Equal(TelegramUserRole.Engineer, binding.MinRole);
            Assert.True(binding.IsLibraryVisible);
            Assert.False(binding.CanUseForDiagnostics);
        }
    }

    [Fact]
    public async Task EfTelegramManualFileBindingStoreDiagnosticLookupOnlyReturnsOwnerManualBindings()
    {
        await using var connection = await OpenConnectionAsync();
        await using var provider = await BuildProviderAsync(connection);
        var store = provider.GetRequiredService<ITelegramManualFileBindingStore>();

        await store.UpsertSeriesAsync(new TelegramManualFileBinding(
            "gree-gmv6-service-manual",
            "telegram-file-id-service",
            "Gree GMV6 Service Manual EN.pdf",
            "application/pdf",
            DateTimeOffset.UtcNow,
            "TelegramManualBind",
            "Owner",
            Brand: "Gree",
            Series: "GMV6"));

        Assert.Null(await store.GetDiagnosticBySeriesAsync("Gree", "GMV6"));

        await store.UpsertSeriesAsync(new TelegramManualFileBinding(
            "gree-gmv6-owner-manual",
            "telegram-file-id-owner",
            "Gree GMV6 Owner Manual.pdf",
            "application/pdf",
            DateTimeOffset.UtcNow,
            "TelegramManualBind",
            "Owner",
            Brand: "Gree",
            Series: "GMV6",
            DocumentType: TelegramLibraryDocumentType.OwnerManual,
            MinRole: TelegramUserRole.Consumer,
            IsLibraryVisible: true,
            CanUseForDiagnostics: true));

        var diagnostic = await store.GetDiagnosticBySeriesAsync("gree", "gmv6");

        Assert.NotNull(diagnostic);
        Assert.Equal("telegram-file-id-owner", diagnostic.TelegramFileId);
        Assert.Equal(TelegramLibraryDocumentType.OwnerManual, diagnostic.DocumentType);
        Assert.True(diagnostic.CanUseForDiagnostics);
    }

    [Fact]
    public async Task EfTelegramManualFileBindingStoreKeepsSeriesDocumentTypesIndependent()
    {
        await using var connection = await OpenConnectionAsync();
        await using var provider = await BuildProviderAsync(connection);
        var store = provider.GetRequiredService<ITelegramManualFileBindingStore>();

        await store.UpsertSeriesAsync(new TelegramManualFileBinding(
            "gree-gmv6-service-manual",
            "telegram-file-id-service",
            "Gree GMV6 Service Manual EN.pdf",
            "application/pdf",
            DateTimeOffset.UtcNow,
            "TelegramManualBind",
            "Owner",
            Brand: "Gree",
            Series: "GMV6"));
        await store.UpsertSeriesAsync(new TelegramManualFileBinding(
            "gree-gmv6-owner-manual",
            "telegram-file-id-owner",
            "Gree GMV6 Owner Manual.pdf",
            "application/pdf",
            DateTimeOffset.UtcNow,
            "TelegramManualBind",
            "Owner",
            Brand: "Gree",
            Series: "GMV6",
            DocumentType: TelegramLibraryDocumentType.OwnerManual,
            MinRole: TelegramUserRole.Consumer,
            CanUseForDiagnostics: true));

        var all = await store.ListAsync();

        Assert.Contains(all, item => item.DocumentType == TelegramLibraryDocumentType.ServiceManual);
        Assert.Contains(all, item => item.DocumentType == TelegramLibraryDocumentType.OwnerManual);
        Assert.Equal(2, all.Count(item =>
            string.Equals(item.Brand, "Gree", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.Series, "GMV6", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task EfTelegramManualFileBindingStoreReplacesActiveSeriesBinding()
    {
        await using var connection = await OpenConnectionAsync();
        await using var provider = await BuildProviderAsync(connection);
        var store = provider.GetRequiredService<ITelegramManualFileBindingStore>();

        await store.UpsertSeriesAsync(new TelegramManualFileBinding(
            "gree-gmv6-service-manual",
            "telegram-file-id-old",
            "Gree GMV6 Service Manual EN.pdf",
            "application/pdf",
            DateTimeOffset.UtcNow,
            "TelegramManualBind",
            "Admin",
            Brand: "Gree",
            Series: "GMV6"));
        await store.UpsertSeriesAsync(new TelegramManualFileBinding(
            "gree-gmv6-service-manual",
            "telegram-file-id-new",
            "Gree GMV6 Service Manual EN Rev C.pdf",
            "application/pdf",
            DateTimeOffset.UtcNow,
            "TelegramManualBind",
            "Admin",
            Brand: "gree",
            Series: "gmv6"));

        var binding = await store.GetBySeriesAsync("Gree", "GMV6");
        var all = await store.ListAsync();

        Assert.Equal("telegram-file-id-new", binding?.TelegramFileId);
        Assert.Single(all, item =>
            string.Equals(item.Brand, "gree", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.Series, "gmv6", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EfTelegramManualBindingModelAndMigrationContainPersistentManualState()
    {
        await using var connection = await OpenConnectionAsync();
        await using var provider = await BuildProviderAsync(connection);
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityType = context.Model.FindEntityType(typeof(TelegramManualBindingEntity));

        Assert.NotNull(entityType);
        Assert.Equal("TelegramManualBindings", entityType.GetTableName());
        Assert.NotNull(entityType.FindProperty(nameof(TelegramManualBindingEntity.TelegramFileId)));
        Assert.NotNull(entityType.FindProperty(nameof(TelegramManualBindingEntity.TelegramFileUniqueId)));
        Assert.NotNull(entityType.FindProperty(nameof(TelegramManualBindingEntity.FileSize)));
        Assert.NotNull(entityType.FindProperty(nameof(TelegramManualBindingEntity.UploadedByTelegramUserId)));
        Assert.NotNull(entityType.FindProperty(nameof(TelegramManualBindingEntity.UploadedByTelegramChatId)));
        Assert.NotNull(entityType.FindProperty(nameof(TelegramManualBindingEntity.Title)));
        Assert.NotNull(entityType.FindProperty(nameof(TelegramManualBindingEntity.DocumentType)));
        Assert.NotNull(entityType.FindProperty(nameof(TelegramManualBindingEntity.MinRole)));
        Assert.NotNull(entityType.FindProperty(nameof(TelegramManualBindingEntity.IsLibraryVisible)));
        Assert.NotNull(entityType.FindProperty(nameof(TelegramManualBindingEntity.CanUseForDiagnostics)));
        Assert.NotNull(entityType.FindProperty(nameof(TelegramManualBindingEntity.IsActive)));
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.Properties.Select(property => property.Name).SequenceEqual(
                [
                    nameof(TelegramManualBindingEntity.Brand),
                    nameof(TelegramManualBindingEntity.Series),
                    nameof(TelegramManualBindingEntity.IsActive)
                ]));

        var migrationsDirectory = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Infrastructure",
            "Persistence",
            "Migrations");
        Assert.Contains(
            Directory.EnumerateFiles(migrationsDirectory, "*AddTelegramManualBindings.cs"),
            path => path.EndsWith("AddTelegramManualBindings.cs", StringComparison.Ordinal));
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
        services.AddSingleton<ITelegramManualFileBindingStore, EfTelegramManualFileBindingStore>();

        var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
        return provider;
    }
}
