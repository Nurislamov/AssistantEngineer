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
    private const string OfficialSupportCatalogManualId = "gree-official-support-error-catalog";

    private static readonly HashSet<string> OfficialSupportGmvCodes = new(
        [
            "A0",
            "C0",
            "C7",
            "E0",
            "E1",
            "F3",
            "H5",
            "L1",
            "o1",
            "P0",
            "P1",
            "P2",
            "U0",
            "U2",
            "U3",
            "U4",
            "U5"
        ],
        StringComparer.OrdinalIgnoreCase);
    private static readonly string KnowledgePath = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree",
        "gmv6",
        "outdoor",
        "h5.json");
    private static readonly string PackagePath = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "packages",
        "gree-gmv6-outdoor-fault-protection-codes.json");

    [Fact]
    public void GreeGmvH5JsonLoadsFromEmbeddedResource()
    {
        var source = new JsonErrorKnowledgeLocalizationSource();

        var entries = source.GetEntries();
        var entry = Assert.Single(entries, item => item.Id == "gree-gmv6-outdoor-h5");

        Assert.Equal(262, entries.Count);
        Assert.Equal("Gree", entry.Manufacturer);
        Assert.Equal(ErrorKnowledgeEquipmentFamily.VRF, entry.EquipmentFamily);
        Assert.Equal(ErrorKnowledgeEquipmentType.OutdoorUnit, entry.EquipmentType);
        Assert.Equal("GMV6", entry.Series);
        Assert.Equal("H5", entry.Code);
        Assert.Equal(ErrorKnowledgeSignalType.Protection, entry.SignalType);
        Assert.Equal(ErrorKnowledgeDisplaySource.OutdoorBoard, entry.DisplaySource);
        Assert.Equal(ErrorKnowledgeSystemPart.Fan, entry.SystemPart);
        Assert.Equal(ErrorKnowledgeSeverity.High, entry.Severity);
        Assert.True(entry.RequiresQualifiedService);
        Assert.False(entry.CanCustomerContinueOperation);
        Assert.Equal("gree-gmv6-outdoor-fault-protection-codes", entry.PackageId);
        Assert.Equal("Over-current protection of inverter fan", entry.SourceMeaning);
        Assert.Equal("ManualVerified", entry.VerificationStatus);
        Assert.Equal("High", entry.Confidence);

        var officialSupportReference = Assert.Single(entry.SourceReferences);
        Assert.Equal("Gree-GMV-H5", officialSupportReference.DocumentCode);
        Assert.Equal(OfficialSupportCatalogManualId, officialSupportReference.ManualId);
        Assert.Equal("Manual", officialSupportReference.SourceType);
        Assert.Equal("ManualVerified", officialSupportReference.VerificationStatus);
        Assert.Equal("High", officialSupportReference.Confidence);

        Assert.Contains(
            JsonErrorKnowledgeLocalizationSource.GetEmbeddedResourceNames(),
            name => name.EndsWith(".gree.gmv6.outdoor.h5.json", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ErrorKnowledgeResourceFilterRejectsUnrelatedEmbeddedJson()
    {
        Assert.True(ErrorKnowledgeJsonLoader.IsKnowledgeResource(
            "AssistantEngineer.Modules.EquipmentDiagnostics.Knowledge.ErrorKnowledge.gree.gmv6.outdoor.h5.json"));
        Assert.False(ErrorKnowledgeJsonLoader.IsKnowledgeResource(
            "AssistantEngineer.Modules.EquipmentDiagnostics.Knowledge.gree.gree-gmv.json"));
        Assert.False(ErrorKnowledgeJsonLoader.IsKnowledgeResource(
            "AssistantEngineer.Modules.EquipmentDiagnostics.Knowledge.ErrorKnowledge.packages.gree-gmv6-outdoor-fault-protection-codes.json"));
        Assert.True(ErrorKnowledgeJsonLoader.IsPackageResource(
            "AssistantEngineer.Modules.EquipmentDiagnostics.Knowledge.ErrorKnowledge.packages.gree-gmv6-outdoor-fault-protection-codes.json"));
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
            Assert.Contains("Entry: gree-gmv6-outdoor-h5", smoke.Output, StringComparison.Ordinal);
            Assert.Contains("Debugging entry: gree-gmv6-debugging-u0", smoke.Output, StringComparison.Ordinal);
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
        var directory = KnowledgeDirectory();

        var entries = new ErrorKnowledgeJsonLoader().LoadFromDirectory(directory);
        var expectedCodes = GmvIduMergedCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var referencedEntries = entries
            .Where(entry => entry.SourceReferences.Any(reference =>
                reference.ManualId == "gree-gmv-idu-service-manual"))
            .ToArray();

        var gmvMiniMergedCodes = GmvMiniMergedCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var gmvMiniReferencedEntries = entries
            .Where(entry => entry.SourceReferences.Any(reference =>
                reference.ManualId == "gree-gmv-mini-service-manual") &&
                !string.Equals(entry.Series, "GMV Mini", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(262, entries.Count);
        Assert.Single(entries, entry => entry.Id == "gree-gmv6-outdoor-h5");
        Assert.Equal(38, referencedEntries.Length);
        var referencedCodes = referencedEntries
            .Select(entry => entry.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Empty(expectedCodes.Except(referencedCodes, StringComparer.OrdinalIgnoreCase));
        Assert.Empty(referencedCodes.Except(expectedCodes, StringComparer.OrdinalIgnoreCase));
        Assert.All(referencedEntries, entry =>
        {
            Assert.Equal("IndoorUnit", entry.EquipmentType.ToString());
            Assert.Equal("IndoorUnit", entry.DisplaySource.ToString());
            var expectedManualIds = new List<string>
            {
                "gree-gmv6-service-manual-2020-09",
                "gree-gmv-idu-service-manual"
            };

            if (gmvMiniMergedCodes.Contains(entry.Code))
            {
                expectedManualIds.Add("gree-gmv-mini-service-manual");
            }

            if (OfficialSupportGmvCodes.Contains(entry.Code))
            {
                expectedManualIds.Add(OfficialSupportCatalogManualId);
            }

            var manualIds = entry.SourceReferences.Select(reference => reference.ManualId!).ToArray();
            Assert.Equal(expectedManualIds, manualIds);

            Assert.Equal("GC202001-I", entry.SourceReferences[0].DocumentCode);
            Assert.Equal("GC202004-X", entry.SourceReferences[1].DocumentCode);
            Assert.All(entry.SourceReferences, reference =>
            {
                var serialized = string.Join(
                    " ",
                    reference.SourceName,
                    reference.SourceReference,
                    reference.Notes);
                Assert.DoesNotContain("SERVICE_MANUAL_GMV_IDU.pdf", serialized, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("D:\\", serialized, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("C:\\", serialized, StringComparison.OrdinalIgnoreCase);
            });
        });
        Assert.Equal(31, gmvMiniReferencedEntries.Length);
        var gmvMiniReferencedCodes = gmvMiniReferencedEntries
            .Select(entry => entry.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var officialSupportReferencedCodes = entries
            .Where(entry => entry.SourceReferences.Any(reference =>
                reference.ManualId == OfficialSupportCatalogManualId))
            .Select(entry => entry.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Empty(gmvMiniMergedCodes.Except(gmvMiniReferencedCodes, StringComparer.OrdinalIgnoreCase));
        Assert.Empty(gmvMiniReferencedCodes.Except(gmvMiniMergedCodes, StringComparer.OrdinalIgnoreCase));
        Assert.All(gmvMiniReferencedEntries, entry =>
        {
            Assert.Contains(
                entry.SourceReferences,
                reference => reference.ManualId == "gree-gmv-mini-service-manual");
            Assert.All(entry.SourceReferences, reference =>
            {
                var serialized = string.Join(
                    " ",
                    reference.SourceName,
                    reference.SourceReference,
                    reference.Notes);
                Assert.DoesNotContain("SERVICE_MANUAL_GMV_MINI (1).pdf", serialized, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("D:\\", serialized, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("C:\\", serialized, StringComparison.OrdinalIgnoreCase);
            });
        });
        Assert.All(
            entries.Where(entry =>
                !expectedCodes.Contains(entry.Code) &&
                !gmvMiniMergedCodes.Contains(entry.Code) &&
                !officialSupportReferencedCodes.Contains(entry.Code) &&
                !entry.Id.StartsWith("gree-gmv-mini-", StringComparison.OrdinalIgnoreCase)),
            entry => Assert.Empty(entry.SourceReferences));
    }

    [Fact]
    public void ImprovedGmv6MessageBatchDoesNotUseGenericWaterOrUnsafeProtectionWording()
    {
        var directory = KnowledgeDirectory();
        var entries = new ErrorKnowledgeJsonLoader().LoadFromDirectory(directory);
        var improvedIds = ImprovedMessageQualityEntryIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var improvedEntries = entries
            .Where(entry => improvedIds.Contains(entry.Id))
            .ToArray();

        Assert.Equal(ImprovedMessageQualityEntryIds.Length, improvedEntries.Length);
        Assert.All(improvedEntries, entry =>
        {
            Assert.Equal("ManualVerified", entry.VerificationStatus);
            Assert.All(entry.Texts, text =>
            {
                var userFacing = string.Join(
                    " ",
                    text.Title,
                    text.Summary,
                    text.SafetyNote,
                    string.Join(" ", text.PossibleCauses),
                    string.Join(" ", text.CheckSteps),
                    string.Join(" ", text.DoNotAdvise),
                    text.RecommendedAction);
                foreach (var forbidden in ImprovedMessageForbiddenPhrases)
                {
                    Assert.DoesNotContain(forbidden, userFacing, StringComparison.OrdinalIgnoreCase);
                }

                Assert.DoesNotContain("Категория:", userFacing, StringComparison.Ordinal);
                Assert.DoesNotContain("Уверенность:", userFacing, StringComparison.Ordinal);
                Assert.DoesNotContain("Источник:", userFacing, StringComparison.Ordinal);
            });
        });

        var u3 = Assert.Single(improvedEntries, entry => entry.Id == "gree-gmv6-debugging-u3");
        Assert.All(u3.Texts, text =>
        {
            var userFacing = string.Join(" ", text.Title, text.Summary, string.Join(" ", text.CheckSteps));
            Assert.Contains("питания", userFacing, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("фаз", userFacing, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("фазиров", userFacing, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void OptionalSourceReferencesLoadWhenPresent()
    {
        var json = Mutate(root =>
            root["sourceReferences"] = new JsonArray(
                SourceReference(
                    sourceName: "Gree GMV6 service manual",
                    documentCode: "GC202001-I",
                    sourceReference: "Manual page 1 / PDF page 2",
                    manualId: "gree-gmv6-service-manual-2020-09",
                    packageId: "gree-gmv6-outdoor-fault-protection-codes"),
                SourceReference(
                    sourceName: "Gree GMV IDU service manual",
                    documentCode: "GC202004-X",
                    sourceReference: "Manual page 173 / PDF page 178",
                    manualId: "gree-gmv-idu-service-manual",
                    packageId: "gree-gmv6-outdoor-fault-protection-codes")));

        var result = Validate(json);

        Assert.True(result.IsValid);
        var entry = Assert.Single(result.Entries);
        Assert.Equal(2, entry.SourceReferences.Count);
        Assert.Equal("GC202001-I", entry.SourceReferences[0].DocumentCode);
        Assert.Equal("gree-gmv-idu-service-manual", entry.SourceReferences[1].ManualId);
    }

    [Fact]
    public void EmptySourceReferencesFailsValidation()
    {
        var json = Mutate(root => root["sourceReferences"] = new JsonArray());

        var result = Validate(json);

        Assert.Contains(
            result.Issues,
            issue => issue.Problem.Contains("sourceReferences must be non-empty", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("sourceName", "sourceName is required")]
    [InlineData("sourceReference", "sourceReference is required")]
    [InlineData("sourceType", "sourceType is required")]
    [InlineData("sourceLanguage", "sourceLanguage is required")]
    [InlineData("verificationStatus", "verificationStatus is required")]
    [InlineData("confidence", "confidence is required")]
    public void SourceReferenceRequiredFieldsFailValidation(string property, string expected)
    {
        var json = Mutate(root =>
        {
            var reference = SourceReference();
            reference.Remove(property);
            root["sourceReferences"] = new JsonArray(reference);
        });

        var result = Validate(json);

        Assert.Contains(
            result.Issues,
            issue => issue.Problem.Contains(expected, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("sourceType", "Blog", "sourceType 'Blog' is invalid")]
    [InlineData("sourceLanguage", "de", "sourceLanguage 'de' is invalid")]
    [InlineData("verificationStatus", "Published", "verificationStatus 'Published' is invalid")]
    [InlineData("confidence", "Certain", "confidence 'Certain' is invalid")]
    public void SourceReferenceControlledValuesFailValidation(
        string property,
        string value,
        string expected)
    {
        var json = Mutate(root =>
        {
            var reference = SourceReference();
            reference[property] = value;
            root["sourceReferences"] = new JsonArray(reference);
        });

        var result = Validate(json);

        Assert.Contains(
            result.Issues,
            issue => issue.Problem.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void SourceReferencePackageIdMustExistWhenPresent()
    {
        var json = Mutate(root =>
            root["sourceReferences"] = new JsonArray(SourceReference(packageId: "missing-package")));

        var result = Validate(json);

        Assert.Contains(
            result.Issues,
            issue => issue.Problem.Contains("sourceReferences packageId 'missing-package' does not reference an existing package", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("sourceName", "token=123456:abcdefghijklmnopqrstuvwxyzABCDE", "token/webhook-secret-like")]
    [InlineData("sourceReference", "callback sr:t:12", "callback payload")]
    [InlineData("notes", "chat 123456789012", "raw chat/platform-user-id-like")]
    public void SourceReferenceSensitivePlatformValuesFailValidation(
        string property,
        string value,
        string expected)
    {
        var json = Mutate(root =>
        {
            var reference = SourceReference();
            reference[property] = value;
            root["sourceReferences"] = new JsonArray(reference);
        });

        var result = Validate(json);

        Assert.Contains(
            result.Issues,
            issue => issue.Problem.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public void GreeGmvProtectionPackageManifestLoadsSuccessfully()
    {
        var result = ValidateRepository();

        Assert.True(result.IsValid);
        Assert.Equal(7, result.Packages.Count);
        var package = Assert.Single(
            result.Packages,
            item => item.PackageId == "gree-gmv6-outdoor-fault-protection-codes");
        Assert.Equal("Gree", package.Manufacturer);
        Assert.Equal(ErrorKnowledgeEquipmentFamily.VRF, package.EquipmentFamily);
        Assert.Equal("GMV6", package.Series);
        Assert.Equal(
            [ErrorKnowledgeSignalType.Fault, ErrorKnowledgeSignalType.Protection],
            package.IntendedSignalTypes);
        Assert.Equal(120, package.EntryCountExpected);
    }

    [Fact]
    public void GmvMiniManualImportAddsExpectedPackagesEntriesAndReferences()
    {
        var result = ValidateRepository();

        Assert.True(result.IsValid);
        Assert.Contains(
            result.Packages,
            package => package.PackageId == "gree-gmv-mini-vrf-indoor-controller-codes" &&
                package.EntryCountExpected == 2);
        Assert.Contains(
            result.Packages,
            package => package.PackageId == "gree-gmv-mini-vrf-outdoor-protection-codes" &&
                package.EntryCountExpected == 1);
        Assert.Contains(
            result.Packages,
            package => package.PackageId == "gree-gmv-mini-vrf-status-codes" &&
                package.EntryCountExpected == 6);

        var gmvMiniEntries = result.Entries
            .Where(entry => string.Equals(entry.Series, "GMV Mini", StringComparison.Ordinal))
            .ToArray();
        var gmvMiniReferencedEntries = result.Entries
            .Where(entry => entry.SourceReferences.Any(reference =>
                reference.ManualId == "gree-gmv-mini-service-manual") &&
                !string.Equals(entry.Series, "GMV Mini", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(9, gmvMiniEntries.Length);
        Assert.Equal(31, gmvMiniReferencedEntries.Length);
        Assert.Contains(gmvMiniEntries, entry => entry.Id == "gree-gmv-mini-indoor-c0");
        Assert.Contains(gmvMiniEntries, entry => entry.Id == "gree-gmv-mini-indoor-aj");
        Assert.Contains(gmvMiniEntries, entry => entry.Id == "gree-gmv-mini-outdoor-ec");
        Assert.Contains(gmvMiniEntries, entry => entry.Id == "gree-gmv-mini-status-a1");
        Assert.All(gmvMiniEntries, entry =>
        {
            Assert.Equal("ManualVerified", entry.VerificationStatus);
            Assert.Contains(
                entry.SourceReferences,
                reference => reference.ManualId == "gree-gmv-mini-service-manual");
            Assert.All(entry.SourceReferences, reference =>
            {
                var serialized = string.Join(
                    " ",
                    reference.SourceName,
                    reference.SourceReference,
                    reference.Notes);
                Assert.DoesNotContain("SERVICE_MANUAL_GMV_MINI (1).pdf", serialized, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("D:\\", serialized, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("C:\\", serialized, StringComparison.OrdinalIgnoreCase);
            });
        });
    }

    [Fact]
    public void GreeH5ConsumerOutputUsesJsonRussianTextAndIsCustomerSafe()
    {
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter(
            new JsonErrorKnowledgeLocalizationSource());

        var text = formatter.FormatConsumer(Response(), hasPhoneNumber: false, maxLength: 2000);

        Assert.Contains("защита по перегрузке тока инверторного вентилятора", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Gree GMV H5", text, StringComparison.Ordinal);
        Assert.DoesNotContain("измерить напряжение", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("добавить хладагент", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("заменить компрессор", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("откройте панели", text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(TelegramUserRole.Installer, "фаз U, V, W")]
    [InlineData(TelegramUserRole.Engineer, "фаз U, V, W")]
    [InlineData(TelegramUserRole.Admin, "фаз U, V, W")]
    [InlineData(TelegramUserRole.Owner, "фаз U, V, W")]
    public void GreeH5TechnicalRolesUseJsonRussianText(
        TelegramUserRole role,
        string expected)
    {
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter(
            new JsonErrorKnowledgeLocalizationSource());

        var text = formatter.FormatTechnical(Response(), role);

        Assert.Contains(expected, text, StringComparison.Ordinal);
        Assert.DoesNotContain("Preliminary diagnostic entry", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("предварительный сигнал", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Черновик / непроверено", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Суть:", text, StringComparison.Ordinal);
        Assert.Contains("Что проверить:", text, StringComparison.Ordinal);
        Assert.Contains("Важно:", text, StringComparison.Ordinal);
        Assert.Contains("Ограничения вывода:", text, StringComparison.Ordinal);
        Assert.Contains("Дальше:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Уверенность:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Источник:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Категория:", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("L0", "GMV6", "Malfunction of IDU")]
    [InlineData("E1", "GMV6", "High-pressure protection")]
    [InlineData("U0", "GMV6", "Preheat time of compressor is insufficient")]
    [InlineData("A0", "GMV6", "Unit waiting for debugging")]
    [InlineData("EC", "GMV Mini", "Loose protection for discharge temperature sensor for compressor 1")]
    [InlineData("A1", "GMV Mini", "Operational parameter inquiry of compressor")]
    public void RepresentativeGmv6CodeResolvesFromManualCatalog(
        string code,
        string series,
        string sourceMeaning)
    {
        var source = new JsonErrorKnowledgeLocalizationSource();
        var response = Response(code, series);

        var selection = source.Select(response, "ru", ErrorKnowledgeAudience.Engineer);

        Assert.NotNull(selection);
        Assert.Equal(sourceMeaning, selection.Entry.SourceMeaning);
        Assert.Equal("ManualVerified", selection.Entry.VerificationStatus);
        Assert.Equal("ru", selection.Text.Locale);
    }

    [Theory]
    [InlineData("U0", EquipmentDiagnosticBotResponseStatus.ReferenceOnly)]
    [InlineData("A0", EquipmentDiagnosticBotResponseStatus.ReferenceOnly)]
    public void ReferenceOnlyRuntimeResponseUsesImportedRussianKnowledge(
        string code,
        EquipmentDiagnosticBotResponseStatus status)
    {
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter(
            new JsonErrorKnowledgeLocalizationSource());

        var text = formatter.FormatTechnical(Response(code, "GMV6", status), TelegramUserRole.Engineer);

        Assert.Contains($"Gree GMV {code}", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Справочное совпадение", text, StringComparison.Ordinal);
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
    public void ManualEntryRequiresExactSourceMeaning()
    {
        var json = Mutate(root => root.Remove("sourceMeaning"));

        var result = Validate(json);

        Assert.Contains(result.Issues, issue =>
            issue.Problem.Contains("sourceMeaning is required for Manual entries", StringComparison.Ordinal));
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

    [Theory]
    [InlineData("Сообщение о связи связи и адресации.", "связи связи")]
    [InlineData("Ошибка ошибка наружного блока.", "ошибка ошибка")]
    [InlineData("Защита защиты компрессора.", "защита защиты")]
    public void AwkwardRussianDuplicateWordingFailsValidation(
        string text,
        string expected)
    {
        var json = Mutate(root => RussianConsumer(root)["summary"] = text);

        var result = Validate(json);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Problem.Contains("awkward duplicate wording", StringComparison.Ordinal) &&
                issue.Problem.Contains(expected, StringComparison.Ordinal));
    }

    private static ErrorKnowledgeValidationResult Validate(
        string json,
        string? packageJson = null) =>
        new ErrorKnowledgeJsonValidator().Validate(
        [
            new("test.json", json),
            new(
                "packages/gree-gmv6-outdoor-fault-protection-codes.json",
                packageJson ?? SingleEntryPackage(File.ReadAllText(PackagePath)))
        ]);

    private static ErrorKnowledgeValidationResult ValidateRepository()
    {
        var sources = Directory
            .EnumerateFiles(KnowledgeDirectory(), "*.json", SearchOption.AllDirectories)
            .Select(path => new ErrorKnowledgeJsonSource(path, File.ReadAllText(path)))
            .ToArray();
        return new ErrorKnowledgeJsonValidator().Validate(sources);
    }

    private static string KnowledgeDirectory() =>
        Path.Combine(
            TestPaths.RepoRoot,
            "data",
            "equipment-diagnostics",
            "error-knowledge");

    private static string SingleEntryPackage(string packageJson)
    {
        var root = JsonNode.Parse(packageJson)!.AsObject();
        root["entryCountExpected"] = 1;
        return root.ToJsonString();
    }

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
        new(
            "packages/gree-gmv6-outdoor-fault-protection-codes.json",
            SingleEntryPackage(File.ReadAllText(PackagePath)));

    private static JsonObject RussianConsumer(JsonObject root) =>
        root["texts"]!
            .AsArray()
            .Select(node => node!.AsObject())
            .Single(node =>
                node["locale"]!.GetValue<string>() == "ru" &&
                node["audience"]!.GetValue<string>() == "Consumer");

    private static readonly string[] GmvIduMergedCodes =
    [
        "L0", "L1", "L2", "L3", "L4", "L5", "L7", "L8", "L9", "LA", "LH", "LC",
        "d1", "d3", "d4", "d6", "d7", "d8", "d9", "dA", "dH", "dC", "dL", "dE",
        "o1", "o2", "o3", "o4", "o5", "o6", "o7", "o8", "o9", "oA", "ob", "oC",
        "o0", "db"
    ];

    private static readonly string[] GmvMiniMergedCodes =
    [
        "L5", "d3", "d4", "d6", "d7", "d8", "d9", "dE",
        "E1", "E3", "F1", "F3", "FP", "J8", "J9", "b2", "b3",
        "C8", "C9", "CA",
        "A3", "A4", "A6", "A7", "A8", "AU", "AH", "AL", "Ad", "nA", "nE"
    ];

    private static readonly string[] ImprovedMessageQualityEntryIds =
    [
        "gree-gmv6-debugging-u3",
        "gree-gmv6-debugging-c0",
        "gree-gmv6-debugging-u0",
        "gree-gmv6-indoor-l1",
        "gree-gmv6-outdoor-h5",
        "gree-gmv6-outdoor-e1",
        "gree-gmv6-status-a0",
        "gree-gmv6-indoor-d1",
        "gree-gmv6-indoor-o1"
    ];

    private static readonly string[] ImprovedMessageForbiddenPhrases =
    [
        "классифицирован",
        "диагностический вывод должен оставаться",
        "если подробная процедура не добавлена",
        "сообщение наладки",
        "штатную процедуру как основное содержание",
        "не обходить защит",
        "не обходить защиты",
        "не отключать защит",
        "не отключать защиты"
    ];

    private static JsonObject SourceReference(
        string sourceName = "Gree GMV6 service manual",
        string? documentCode = "GC202001-I",
        string sourceReference = "Manual page 1 / PDF page 2",
        string sourceType = "Manual",
        string sourceLanguage = "en",
        string verificationStatus = "ManualVerified",
        string confidence = "High",
        string? manualId = "gree-gmv6-service-manual-2020-09",
        string? packageId = null,
        string? notes = "Synthetic test source reference.") =>
        new()
        {
            ["sourceName"] = sourceName,
            ["documentCode"] = documentCode,
            ["sourceReference"] = sourceReference,
            ["sourceType"] = sourceType,
            ["sourceLanguage"] = sourceLanguage,
            ["verificationStatus"] = verificationStatus,
            ["confidence"] = confidence,
            ["manualId"] = manualId,
            ["packageId"] = packageId,
            ["notes"] = notes
        };

    private static EquipmentDiagnosticBotResponse Response(
        string code = "H5",
        string series = "GMV",
        EquipmentDiagnosticBotResponseStatus status = EquipmentDiagnosticBotResponseStatus.Answer) =>
        new(
            status,
            "English title",
            "English message",
            "Gree",
            code,
            new EquipmentDiagnosticBotEquipmentContext(
                "Gree",
                series,
                null,
                EquipmentCategory.VrfOutdoorUnit,
                EquipmentDiagnosticBotEquipmentSide.Outdoor,
                EquipmentDiagnosticBotDisplayContext.OduMainBoardLed),
            new EquipmentDiagnosticBotObservedCodeContext(code, code, null),
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
