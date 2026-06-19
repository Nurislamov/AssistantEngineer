using System.Reflection;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;

public sealed class ErrorKnowledgeJsonLoader
{
    public const string ResourceMarker = ".Knowledge.ErrorKnowledge.";

    private readonly ErrorKnowledgeJsonValidator _validator;

    public ErrorKnowledgeJsonLoader()
        : this(new ErrorKnowledgeJsonValidator())
    {
    }

    public ErrorKnowledgeJsonLoader(ErrorKnowledgeJsonValidator validator)
    {
        _validator = validator;
    }

    public IReadOnlyCollection<ErrorKnowledgeEntryV2> LoadFromAssembly(Assembly assembly)
    {
        var sources = assembly
            .GetManifestResourceNames()
            .Where(IsKnowledgeResource)
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name =>
            {
                using var stream = assembly.GetManifestResourceStream(name)
                    ?? throw new InvalidOperationException($"Embedded resource '{name}' was not found.");
                using var reader = new StreamReader(stream);
                return new ErrorKnowledgeJsonSource(name, reader.ReadToEnd());
            })
            .ToArray();

        if (sources.Length == 0)
        {
            throw new InvalidOperationException(
                $"No embedded error knowledge JSON resources were found in {assembly.GetName().Name}.");
        }

        return RequireValid(_validator.Validate(sources));
    }

    public IReadOnlyCollection<ErrorKnowledgeEntryV2> LoadFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Error knowledge directory was not found: {directory}");
        }

        var sources = Directory
            .EnumerateFiles(directory, "*.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => new ErrorKnowledgeJsonSource(path, File.ReadAllText(path)))
            .ToArray();
        return RequireValid(_validator.Validate(sources));
    }

    public static bool IsKnowledgeResource(string resourceName) =>
        resourceName.Contains(ResourceMarker, StringComparison.Ordinal) &&
        resourceName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyCollection<ErrorKnowledgeEntryV2> RequireValid(
        ErrorKnowledgeValidationResult result)
    {
        if (result.IsValid)
        {
            return result.Entries;
        }

        throw new InvalidOperationException(
            "Error knowledge validation failed:" + Environment.NewLine +
            string.Join(
                Environment.NewLine,
                result.Issues.Select(issue => $"- {issue.Path}: {issue.Problem}")));
    }
}
