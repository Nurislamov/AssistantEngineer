using System.Diagnostics;
using System.Text.Json.Nodes;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class ErrorKnowledgeJsonValidationTests
{
    private static readonly string KnowledgePath = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree",
        "gmv",
        "h5.json");
    private static readonly string PackagePath = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "packages",
        "gree-gmv-vrf-protection-codes.json");

    [Fact]
    public void GreeGmvH5JsonLoadsFromEmbeddedResource()
    {
        var source = new JsonErrorKnowledgeLocalizationSource();

        var entry = Assert.Single(source.GetEntries());

        Assert.Equal("gree-gmv-h5", entry.Id);
        Assert.Equal("Gree", entry.Manufacturer);
        Assert.Equal(ErrorKnowledgeEquipmentFamily.VRF, entry.EquipmentFamily);
        Assert.Equal(ErrorKnowledgeEquipmentType.OutdoorUnit, entry.EquipmentType);
        Assert.Equal("GMV", entry.Series);
        Assert.Equal("H5", entry.Code);
        Assert.Equal(ErrorKnowledgeSignalType.Protection, entry.SignalType);
        Assert.Equal(ErrorKnowledgeDisplaySource.OutdoorBoard, entry.DisplaySource);
        Assert.Equal(ErrorKnowledgeSystemPart.ProtectionCircuit, entry.SystemPart);
        Assert.Equal(ErrorKnowledgeSeverity.Medium, entry.Severity);
        Assert.True(entry.RequiresQualifiedService);
        Assert.Null(entry.CanCustomerContinueOperation);
        Assert.Equal("gree-gmv-vrf-protection-codes", entry.PackageId);
        Assert.Contains(
            JsonErrorKnowledgeLocalizationSource.GetEmbeddedResourceNames(),
            name => name.EndsWith(".gree.gmv.h5.json", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ErrorKnowledgeResourceFilterRejectsUnrelatedEmbeddedJson()
    {
        Assert.True(ErrorKnowledgeJsonLoader.IsKnowledgeResource(
            "AssistantEngineer.Modules.EquipmentDiagnostics.Knowledge.ErrorKnowledge.gree.gmv.h5.json"));
        Assert.False(ErrorKnowledgeJsonLoader.IsKnowledgeResource(
            "AssistantEngineer.Modules.EquipmentDiagnostics.Knowledge.gree.gree-gmv.json"));
        Assert.False(ErrorKnowledgeJsonLoader.IsKnowledgeResource(
            "AssistantEngineer.Modules.EquipmentDiagnostics.Knowledge.ErrorKnowledge.packages.gree-gmv-vrf-protection-codes.json"));
        Assert.True(ErrorKnowledgeJsonLoader.IsPackageResource(
            "AssistantEngineer.Modules.EquipmentDiagnostics.Knowledge.ErrorKnowledge.packages.gree-gmv-vrf-protection-codes.json"));
        Assert.False(ErrorKnowledgeJsonLoader.IsKnowledgeResource(
            "AssistantEngineer.Modules.EquipmentDiagnostics.Knowledge.staging.example.json"));
        Assert.False(ErrorKnowledgeJsonLoader.IsKnowledgeResource(
            "AssistantEngineer.Modules.EquipmentDiagnostics.some-other.json"));
    }

    [Fact]
    public void BackendDockerBuildContextIncludesRepositoryErrorKnowledge()
    {
        var dockerfile = File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot,
            "deploy",
            "docker",
            "backend",
            "Dockerfile"));

        Assert.Contains(
            "COPY data/equipment-diagnostics/error-knowledge/ ./data/equipment-diagnostics/error-knowledge/",
            dockerfile,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublishedApiAssemblyLoadsEmbeddedGreeH5()
    {
        var publishDirectory = Path.Combine(
            Path.GetTempPath(),
            $"assistant-engineer-published-knowledge-{Guid.NewGuid():N}");
        try
        {
            var result = await RunProcessAsync(
                "dotnet",
                [
                    "publish",
                    "src/Backend/AssistantEngineer.Api/AssistantEngineer.Api.csproj",
                    "--configuration",
                    "Debug",
                    "--no-restore",
                    "--no-build",
                    "--output",
                    publishDirectory,
                    "/p:UseAppHost=false"
                ]);
            Assert.True(
                result.ExitCode == 0,
                $"dotnet publish failed.{Environment.NewLine}{result.Output}");

            var modulePath = Path.Combine(
                publishDirectory,
                "AssistantEngineer.Modules.EquipmentDiagnostics.dll");
            Assert.True(File.Exists(modulePath), $"Published module was not found: {modulePath}");

            var smoke = await RunProcessAsync(
                "dotnet",
                [
                    "run",
                    "--project",
                    "tools/AssistantEngineer.Tools.EquipmentDiagnosticsVerification",
                    "--no-restore",
                    "--",
                    "verify-published-knowledge",
                    "--assembly",
                    modulePath
                ]);
            Assert.True(
                smoke.ExitCode == 0,
                $"Published knowledge smoke failed.{Environment.NewLine}{smoke.Output}");
            Assert.Contains("PASS", smoke.Output, StringComparison.Ordinal);
            Assert.Contains("Entry: gree-gmv-h5", smoke.Output, StringComparison.Ordinal);
            Assert.Contains("Package resource:", smoke.Output, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(publishDirectory))
            {
                Directory.Delete(publishDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void RepositoryKnowledgeDirectoryLoadsSuccessfully()
    {
        var directory = Path.GetDirectoryName(
            Path.GetDirectoryName(
                Path.GetDirectoryName(KnowledgePath)))!;

        var entries = new ErrorKnowledgeJsonLoader().LoadFromDirectory(directory);

        Assert.Single(entries, entry => entry.Id == "gree-gmv-h5");
    }

    [Fact]
    public void GreeGmvProtectionPackageManifestLoadsSuccessfully()
    {
        var result = Validate(File.ReadAllText(KnowledgePath));

        var package = Assert.Single(result.Packages);
        Assert.Equal("gree-gmv-vrf-protection-codes", package.PackageId);
        Assert.Equal("Gree", package.Manufacturer);
        Assert.Equal(ErrorKnowledgeEquipmentFamily.VRF, package.EquipmentFamily);
        Assert.Equal("GMV", package.Series);
        Assert.Equal([ErrorKnowledgeSignalType.Protection], package.IntendedSignalTypes);
        Assert.Equal(1, package.EntryCountExpected);
    }

    [Fact]
    public void GreeH5ConsumerOutputUsesJsonRussianTextAndIsCustomerSafe()
    {
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter(
            new JsonErrorKnowledgeLocalizationSource());

        var text = formatter.FormatConsumer(Response(), hasPhoneNumber: false, maxLength: 2000);

        Assert.Contains("Сработала защита оборудования", text, StringComparison.Ordinal);
        Assert.Contains("Код H5 указывает на защитное состояние системы", text, StringComparison.Ordinal);
        Assert.DoesNotContain("измерить напряжение", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("добавить хладагент", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("заменить компрессор", text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(TelegramUserRole.Installer, "Точное значение необходимо сверить")]
    [InlineData(TelegramUserRole.Engineer, "не как подтверждённый отказ")]
    [InlineData(TelegramUserRole.Admin, "не как подтверждённый отказ")]
    [InlineData(TelegramUserRole.Owner, "не как подтверждённый отказ")]
    public void GreeH5TechnicalRolesUseJsonRussianText(
        TelegramUserRole role,
        string expected)
    {
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter(
            new JsonErrorKnowledgeLocalizationSource());

        var text = formatter.FormatTechnical(Response(), role);

        Assert.Contains(expected, text, StringComparison.Ordinal);
        Assert.DoesNotContain("Preliminary diagnostic entry", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingRequiredRussianAudienceFailsValidation()
    {
        var json = Mutate(root =>
        {
            var texts = root["texts"]!.AsArray();
            var consumer = texts.Single(node =>
                node!["locale"]!.GetValue<string>() == "ru" &&
                node["audience"]!.GetValue<string>() == "Consumer");
            texts.Remove(consumer);
        });

        var result = Validate(json);

        Assert.Contains(
            result.Issues,
            issue => issue.Problem.Contains("missing required ru text for audience Consumer", StringComparison.Ordinal));
    }

    [Fact]
    public void EnglishUiLabelInRussianTextFailsValidation()
    {
        var json = Mutate(root => RussianConsumer(root)["summary"] = "Safety: остановите оборудование.");

        var result = Validate(json);

        Assert.Contains(
            result.Issues,
            issue => issue.Problem.Contains("English UI label 'Safety'", StringComparison.Ordinal));
    }

    [Fact]
    public void UnsafeConsumerInstructionFailsValidation()
    {
        var json = Mutate(root =>
            RussianConsumer(root)["checkSteps"] = new JsonArray("Измерить напряжение на клеммах."));

        var result = Validate(json);

        Assert.Contains(
            result.Issues,
            issue => issue.Problem.Contains("unsafe advice", StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateKnowledgeKeyFailsValidation()
    {
        var json = File.ReadAllText(KnowledgePath);

        var result = new ErrorKnowledgeJsonValidator().Validate(
        [
            new("first.json", json),
            new("second.json", json),
            PackageSource()
        ]);

        Assert.Contains(
            result.Issues,
            issue => issue.Problem.Contains("duplicate entry taxonomy key", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("equipmentFamily", "UnknownFamily", "equipmentFamily 'UnknownFamily' is invalid")]
    [InlineData("equipmentType", "Condenser", "equipmentType 'Condenser' is invalid")]
    [InlineData("signalType", "Alarm", "signalType 'Alarm' is invalid")]
    [InlineData("displaySource", "Display", "displaySource 'Display' is invalid")]
    [InlineData("systemPart", "Valve", "systemPart 'Valve' is invalid")]
    [InlineData("severity", "Emergency", "severity 'Emergency' is invalid")]
    public void UnknownTaxonomyValueFailsValidation(
        string property,
        string value,
        string expected)
    {
        var json = Mutate(root => root[property] = value);

        var result = Validate(json);

        Assert.Contains(result.Issues, issue => issue.Problem.Contains(expected, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("equipmentFamily")]
    [InlineData("equipmentType")]
    [InlineData("signalType")]
    [InlineData("displaySource")]
    [InlineData("systemPart")]
    [InlineData("severity")]
    [InlineData("requiresQualifiedService")]
    [InlineData("packageId")]
    public void MissingTaxonomyFieldFailsValidation(string property)
    {
        var json = Mutate(root => root.Remove(property));

        var result = Validate(json);

        Assert.Contains(result.Issues, issue =>
            issue.Problem.Contains($"{property} is required", StringComparison.Ordinal) ||
            issue.Problem.Contains($"{property} must be present", StringComparison.Ordinal));
    }

    [Fact]
    public void EntryPackageIdMustExist()
    {
        var json = Mutate(root => root["packageId"] = "missing-package");

        var result = Validate(json);

        Assert.Contains(result.Issues, issue =>
            issue.Problem.Contains("does not reference an existing package", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("manufacturer", "Other", "manufacturer does not match")]
    [InlineData("equipmentFamily", "Chiller", "equipmentFamily does not match")]
    [InlineData("series", "Other", "series does not match")]
    public void PackageCompatibilityMismatchFailsValidation(
        string property,
        string value,
        string expected)
    {
        var packageJson = MutatePackage(root => root[property] = value);

        var result = Validate(File.ReadAllText(KnowledgePath), packageJson);

        Assert.Contains(result.Issues, issue => issue.Problem.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void EntrySignalTypeMustBeAllowedByPackage()
    {
        var packageJson = MutatePackage(root =>
            root["intendedSignalTypes"] = new JsonArray("Fault"));

        var result = Validate(File.ReadAllText(KnowledgePath), packageJson);

        Assert.Contains(result.Issues, issue =>
            issue.Problem.Contains("signalType Protection is not allowed", StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicatePackageIdFailsValidation()
    {
        var packageJson = File.ReadAllText(PackagePath);

        var result = new ErrorKnowledgeJsonValidator().Validate(
        [
            new("entry.json", File.ReadAllText(KnowledgePath)),
            new("packages/first.json", packageJson),
            new("packages/second.json", packageJson)
        ]);

        Assert.Contains(result.Issues, issue =>
            issue.Problem.Contains("duplicate packageId", StringComparison.Ordinal));
    }

    [Fact]
    public void PackageEntryCountExpectedMismatchFailsValidation()
    {
        var packageJson = MutatePackage(root => root["entryCountExpected"] = 2);

        var result = Validate(File.ReadAllText(KnowledgePath), packageJson);

        Assert.Contains(result.Issues, issue =>
            issue.Problem.Contains("actual package entry count is 1", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("locale", "fr", "locale 'fr' is invalid")]
    [InlineData("audience", "Technician", "audience 'Technician' is invalid")]
    [InlineData("confidence", "Certain", "confidence 'Certain' is invalid")]
    [InlineData("verificationStatus", "Published", "verificationStatus 'Published' is invalid")]
    public void InvalidControlledValueFailsValidation(
        string property,
        string value,
        string expected)
    {
        var json = Mutate(root =>
        {
            if (property is "locale" or "audience")
            {
                RussianConsumer(root)[property] = value;
            }
            else
            {
                root[property] = value;
            }
        });

        var result = Validate(json);

        Assert.Contains(
            result.Issues,
            issue => issue.Problem.Contains(expected, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Позвоните +998 90 123 45 67.", "phone-number-like")]
    [InlineData("token=123456:abcdefghijklmnopqrstuvwxyzABCDE", "token/webhook-secret-like")]
    [InlineData("Нажмите callback sr:t:12.", "callback payload")]
    [InlineData("Пользователь 123456789012.", "raw chat/platform-user-id-like")]
    public void SensitivePlatformValueFailsValidation(string text, string expected)
    {
        var json = Mutate(root => RussianConsumer(root)["summary"] = text);

        var result = Validate(json);

        Assert.Contains(
            result.Issues,
            issue => issue.Problem.Contains(expected, StringComparison.Ordinal));
    }

    private static ErrorKnowledgeValidationResult Validate(
        string json,
        string? packageJson = null) =>
        new ErrorKnowledgeJsonValidator().Validate(
        [
            new("test.json", json),
            new("packages/gree-gmv-vrf-protection-codes.json", packageJson ?? File.ReadAllText(PackagePath))
        ]);

    private static string Mutate(Action<JsonObject> mutation)
    {
        var root = JsonNode.Parse(File.ReadAllText(KnowledgePath))!.AsObject();
        mutation(root);
        return root.ToJsonString();
    }

    private static string MutatePackage(Action<JsonObject> mutation)
    {
        var root = JsonNode.Parse(File.ReadAllText(PackagePath))!.AsObject();
        mutation(root);
        return root.ToJsonString();
    }

    private static ErrorKnowledgeJsonSource PackageSource() =>
        new("packages/gree-gmv-vrf-protection-codes.json", File.ReadAllText(PackagePath));

    private static JsonObject RussianConsumer(JsonObject root) =>
        root["texts"]!
            .AsArray()
            .Select(node => node!.AsObject())
            .Single(node =>
                node["locale"]!.GetValue<string>() == "ru" &&
                node["audience"]!.GetValue<string>() == "Consumer");

    private static EquipmentDiagnosticBotResponse Response() =>
        new(
            EquipmentDiagnosticBotResponseStatus.Answer,
            "English title",
            "English message",
            "Gree",
            "H5",
            new EquipmentDiagnosticBotEquipmentContext(
                "Gree",
                "GMV",
                null,
                EquipmentCategory.VrfOutdoorUnit,
                EquipmentDiagnosticBotEquipmentSide.Outdoor,
                EquipmentDiagnosticBotDisplayContext.OduMainBoardLed),
            new EquipmentDiagnosticBotObservedCodeContext("H5", "H5", null),
            new EquipmentDiagnosticBotAnswerCard(
                "English title",
                "English summary",
                "English verification",
                [],
                [],
                [],
                [],
                []),
            null,
            new EquipmentDiagnosticBotSourceCard(
                "SeededEngineeringKnowledge",
                "UnverifiedSeed",
                "English source",
                null,
                null,
                null,
                null,
                null,
                []),
            new EquipmentDiagnosticBotSafetyCard("English safety", []),
            true,
            DiagnosticConfidence.Low,
            false,
            true,
            [],
            [],
            null);

    private static async Task<ProcessResult> RunProcessAsync(
        string fileName,
        IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = TestPaths.RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start {fileName}.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return new ProcessResult(
            process.ExitCode,
            string.Concat(await outputTask, Environment.NewLine, await errorTask));
    }

    private sealed record ProcessResult(int ExitCode, string Output);
}
