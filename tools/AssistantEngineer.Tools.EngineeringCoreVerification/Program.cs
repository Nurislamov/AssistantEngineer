namespace AssistantEngineer.Tools.EngineeringCoreVerification;

internal static class Program
{
    public static int Main(string[] args)
    {
        var reportWriter = new EngineeringCoreVerificationReportWriter();

        try
        {
            var fileSystem = new EngineeringCoreVerificationFileSystem();
            var processRunner = new EngineeringCoreVerificationProcessRunner();
            var commandHandler = new EngineeringCoreVerificationCommandHandler(processRunner, reportWriter);
            var policyGuards = new EngineeringCoreVerificationPolicyGuards(fileSystem);
            var commandRouter = new EngineeringCoreVerificationCommandRouter(fileSystem, policyGuards, commandHandler, reportWriter);

            return commandRouter.Run(args);
        }
        catch (Exception exception)
        {
            reportWriter.WriteUnhandledError(exception);
            return 1;
        }
    }
}
