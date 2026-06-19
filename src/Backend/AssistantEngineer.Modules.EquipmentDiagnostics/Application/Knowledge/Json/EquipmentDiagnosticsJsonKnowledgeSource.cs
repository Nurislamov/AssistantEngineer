using System.Reflection;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;

public sealed class EquipmentDiagnosticsJsonKnowledgeSource : IEquipmentDiagnosticsKnowledgeSource
{
    private readonly Lazy<IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry>> _entries;

    public EquipmentDiagnosticsJsonKnowledgeSource()
        : this(new EquipmentDiagnosticsKnowledgeJsonLoader())
    {
    }

    public EquipmentDiagnosticsJsonKnowledgeSource(EquipmentDiagnosticsKnowledgeJsonLoader loader)
    {
        _entries = new Lazy<IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry>>(
            () => loader.LoadFromAssembly(typeof(EquipmentDiagnosticsJsonKnowledgeSource).Assembly),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> GetEntries() =>
        _entries.Value;

    public static IReadOnlyCollection<string> GetEmbeddedKnowledgeResourceNames() =>
        typeof(EquipmentDiagnosticsJsonKnowledgeSource).Assembly
            .GetManifestResourceNames()
            .Where(name =>
                name.Contains(".Knowledge.", StringComparison.Ordinal) &&
                name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains(".Knowledge.ErrorKnowledge.", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains(".Knowledge.staging.", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains(".Knowledge.manual-codebook.", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
}
