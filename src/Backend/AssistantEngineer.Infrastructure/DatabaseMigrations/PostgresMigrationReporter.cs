namespace AssistantEngineer.Infrastructure.DatabaseMigrations;

public interface IPostgresMigrationReporter
{
    void Info(string message);

    void Error(string message);
}

public sealed class ConsolePostgresMigrationReporter : IPostgresMigrationReporter
{
    private readonly TextWriter _output;
    private readonly TextWriter _error;

    public ConsolePostgresMigrationReporter(TextWriter output, TextWriter error)
    {
        _output = output;
        _error = error;
    }

    public void Info(string message) => _output.WriteLine(message);

    public void Error(string message) => _error.WriteLine(message);
}
