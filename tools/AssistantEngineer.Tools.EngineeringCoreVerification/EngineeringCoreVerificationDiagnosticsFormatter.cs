namespace AssistantEngineer.Tools.EngineeringCoreVerification;

internal static class EngineeringCoreVerificationDiagnosticsFormatter
{
    public static string FormatDuration(TimeSpan duration) =>
        duration.TotalMinutes >= 1
            ? $"{duration:mm\\:ss\\.fff}"
            : $"{duration:ss\\.fff}s";

    public static IReadOnlyList<string> BuildSummaryLines(IReadOnlyCollection<StepResult> results)
    {
        if (results.Count == 0)
            return [];

        var lines = new List<string> { "Verification summary:" };

        foreach (var result in results)
        {
            var status = result.ExitCode == 0 ? "OK" : "FAIL";
            lines.Add($"- {status,-4} {result.Name} ({FormatDuration(result.Duration)})");
        }

        var totalDuration = TimeSpan.FromTicks(results.Sum(item => item.Duration.Ticks));
        lines.Add($"Total duration: {FormatDuration(totalDuration)}");
        lines.Add("Slowest 5 steps:");

        foreach (var result in results.OrderByDescending(item => item.Duration).Take(5))
            lines.Add($"- {result.Name}: {FormatDuration(result.Duration)}");

        return lines;
    }
}
