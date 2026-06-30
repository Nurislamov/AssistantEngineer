using AssistantEngineer.Infrastructure.Persistence;
using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Tests.Persistence;

public sealed class HourlyScheduleValueComparerTests
{
    [Fact]
    public async Task FactorsComparerSupportsMetadataSnapshotAndSequenceEquality()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = await CreateContextAsync(connection);
        var property = context.Model
            .FindEntityType(typeof(HourlySchedule))!
            .FindProperty(nameof(HourlySchedule.Factors))!;
        var comparer = property.GetValueComparer();
        IReadOnlyList<double> original = Enumerable.Range(0, 24).Select(index => index / 24.0).ToArray();
        IReadOnlyList<double> equalCopy = original.ToList();

        Assert.NotNull(comparer);
        Assert.True(comparer.Equals(original, equalCopy));
        Assert.Equal(comparer.GetHashCode(original), comparer.GetHashCode(equalCopy));

        var snapshot = Assert.IsAssignableFrom<IReadOnlyList<double>>(comparer.Snapshot(original));

        Assert.Equal(original, snapshot);
        Assert.NotSame(original, snapshot);
    }

    [Fact]
    public async Task FactorsComparerTracksElementChangesWithoutFalseEquivalentReplacement()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = await CreateContextAsync(connection);
        var factors = Enumerable.Repeat(0.5, 24).ToArray();
        var schedule = HourlySchedule.Create("Tracked schedule", factors).Value;
        context.HourlySchedules.Add(schedule);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var persisted = await context.HourlySchedules.SingleAsync();

        Assert.Equal(factors, persisted.Factors);

        var factorsEntry = context.Entry(persisted).Property(item => item.Factors);
        factorsEntry.CurrentValue = persisted.Factors.ToArray();
        context.ChangeTracker.DetectChanges();

        Assert.False(factorsEntry.IsModified);

        var mutableFactors = Assert.IsType<double[]>(persisted.Factors);
        mutableFactors[7] = 0.75;
        context.ChangeTracker.DetectChanges();

        Assert.True(factorsEntry.IsModified);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var updated = await context.HourlySchedules.SingleAsync();

        Assert.Equal(0.75, updated.Factors[7]);
        Assert.Equal(24, updated.Factors.Count);
    }

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<AppDbContext> CreateContextAsync(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }
}
