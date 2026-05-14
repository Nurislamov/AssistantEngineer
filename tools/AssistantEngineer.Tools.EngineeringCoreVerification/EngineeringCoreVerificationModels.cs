namespace AssistantEngineer.Tools.EngineeringCoreVerification;

internal sealed record VerificationStep(
    string Name,
    string FileName,
    string Arguments);

internal sealed record StepResult(
    string Name,
    int ExitCode,
    TimeSpan Duration);

internal sealed record VerificationOptions(
    bool SkipFrontend,
    bool SkipFullDotnet,
    bool Fast,
    bool NoRestore,
    bool NoBuild)
{
    public static VerificationOptions Parse(IReadOnlyCollection<string> args) =>
        new(
            SkipFrontend: Has(args, "--skip-frontend") || Has(args, "-SkipFrontend"),
            SkipFullDotnet: Has(args, "--skip-full-dotnet") || Has(args, "-SkipFullDotnet"),
            Fast: Has(args, "--fast") || Has(args, "-Fast"),
            NoRestore: Has(args, "--no-restore") || Has(args, "-NoRestore"),
            NoBuild: Has(args, "--no-build") || Has(args, "-NoBuild"));

    private static bool Has(IReadOnlyCollection<string> args, string option) =>
        args.Any(arg => string.Equals(arg, option, StringComparison.OrdinalIgnoreCase));
}
