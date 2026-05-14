using System.Diagnostics;

namespace AssistantEngineer.Tools.EngineeringCoreVerification;

internal sealed class EngineeringCoreVerificationCommandHandler(
    IEngineeringCoreVerificationProcessRunner processRunner,
    EngineeringCoreVerificationReportWriter reportWriter)
{
    public StepResult Execute(VerificationStep step)
    {
        reportWriter.WriteStepStart(step.Name);

        var stopwatch = Stopwatch.StartNew();
        var exitCode = processRunner.RunProcess(step.FileName, step.Arguments);
        stopwatch.Stop();

        if (exitCode != 0)
        {
            reportWriter.WriteStepFailure(step.Name, stopwatch.Elapsed);
            return new StepResult(step.Name, exitCode, stopwatch.Elapsed);
        }

        reportWriter.WriteStepSuccess(step.Name, stopwatch.Elapsed);
        return new StepResult(step.Name, 0, stopwatch.Elapsed);
    }
}
