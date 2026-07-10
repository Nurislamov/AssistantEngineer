using System;
using System.IO;
using System.Linq;
using AssistantEngineer.GreeAliceBridge.Contracts.Pilot;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.HttpSmoke;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceReleaseReadinessAuditTests
{
    [Fact]
    public void ReleaseReadinessDocumentsExistAndReadmeLinksThem()
    {
        AssertRepoFileExists("docs", "integrations", "gree-alice", "release-readiness-audit.md");
        AssertRepoFileExists("docs", "integrations", "gree-alice", "internal-offline-rc-checklist.md");
        AssertRepoFileExists("docs", "integrations", "gree-alice", "internal-offline-release-notes-draft.md");

        string readme = ReadRepoFile("docs", "integrations", "gree-alice", "README.md");

        Assert.Contains("release-readiness-audit.md", readme, StringComparison.Ordinal);
        Assert.Contains("internal-offline-rc-checklist.md", readme, StringComparison.Ordinal);
        Assert.Contains("internal-offline-release-notes-draft.md", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void AuditRecordsHonestReleaseStatusAndReleaseOrientedNextStage()
    {
        string audit = ReadRepoFile("docs", "integrations", "gree-alice", "release-readiness-audit.md");

        Assert.Contains("Current Yandex Smart Home production release status: NOT READY.", audit, StringComparison.Ordinal);
        Assert.Contains("Current internal/offline engineering release status: NEAR READY.", audit, StringComparison.Ordinal);
        Assert.Contains("Internal/offline engineering release", audit, StringComparison.Ordinal);
        Assert.Contains("Real Yandex Smart Home production release", audit, StringComparison.Ordinal);
        Assert.Contains("## Critical blockers", audit, StringComparison.Ordinal);
        Assert.Contains("## Shortest RC path", audit, StringComparison.Ordinal);
        Assert.Contains("GREE-ALICE-RC1", audit, StringComparison.Ordinal);
        Assert.Contains("Recommended next stage", audit, StringComparison.Ordinal);
    }

    [Fact]
    public void AuditCoversRequiredProductionBlockers()
    {
        string audit = ReadRepoFile("docs", "integrations", "gree-alice", "release-readiness-audit.md");

        Assert.Contains("real Yandex provider registration", audit, StringComparison.Ordinal);
        Assert.Contains("real OAuth implementation", audit, StringComparison.Ordinal);
        Assert.Contains("production endpoint", audit, StringComparison.Ordinal);
        Assert.Contains("secure secret storage", audit, StringComparison.Ordinal);
        Assert.Contains("production deployment", audit, StringComparison.Ordinal);
        Assert.Contains("live Gree+ read-only approval", audit, StringComparison.Ordinal);
        Assert.Contains("live control approval", audit, StringComparison.Ordinal);
    }

    [Fact]
    public void AuditPostponesNonRcScope()
    {
        string audit = ReadRepoFile("docs", "integrations", "gree-alice", "release-readiness-audit.md");

        Assert.Contains("live Gree+ control", audit, StringComparison.Ordinal);
        Assert.Contains("MQTT", audit, StringComparison.Ordinal);
        Assert.Contains("multi-account rollout", audit, StringComparison.Ordinal);
        Assert.Contains("production deploy", audit, StringComparison.Ordinal);
        Assert.Contains("admin UI", audit, StringComparison.Ordinal);
        Assert.Contains("automatic Gree Cloud discovery auto-expose", audit, StringComparison.Ordinal);
        Assert.Contains("broad VRF control", audit, StringComparison.Ordinal);
    }

    [Fact]
    public void RcChecklistRequiresValidationSmokeAndBansProductionClaims()
    {
        string checklist = ReadRepoFile("docs", "integrations", "gree-alice", "internal-offline-rc-checklist.md");

        Assert.Contains("RC status: CUT LOCALLY / PUSH PENDING", checklist, StringComparison.Ordinal);
        Assert.Contains("RC name: GREE-ALICE-RC1", checklist, StringComparison.Ordinal);
        Assert.Contains("Base commit: b60fb382", checklist, StringComparison.Ordinal);
        Assert.Contains("Scope: internal/offline engineering release candidate", checklist, StringComparison.Ordinal);
        Assert.Contains("Production Yandex release status: NOT READY", checklist, StringComparison.Ordinal);
        Assert.Contains("Full validation PASS", checklist, StringComparison.Ordinal);
        Assert.Contains("Local smoke harness PASS", checklist, StringComparison.Ordinal);
        Assert.Contains("Local HTTP smoke PASS if local API available", checklist, StringComparison.Ordinal);
        Assert.Contains("No production claims", checklist, StringComparison.Ordinal);
        Assert.Contains("No real Yandex calls", checklist, StringComparison.Ordinal);
        Assert.Contains("No real OAuth credentials", checklist, StringComparison.Ordinal);
        Assert.Contains("No real tokens", checklist, StringComparison.Ordinal);
        Assert.Contains("No live Gree+ Cloud", checklist, StringComparison.Ordinal);
        Assert.Contains("No MQTT", checklist, StringComparison.Ordinal);
        Assert.Contains("No device control", checklist, StringComparison.Ordinal);
        Assert.Contains("No production deployment", checklist, StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseNotesDraftContainsRequiredSectionsAndSafetyBoundaries()
    {
        string notes = ReadRepoFile("docs", "integrations", "gree-alice", "internal-offline-release-notes-draft.md");

        Assert.Contains("Status: RC1 / INTERNAL OFFLINE RELEASE CANDIDATE / NOT PRODUCTION", notes, StringComparison.Ordinal);
        Assert.Contains("Release name: GREE-ALICE-RC1", notes, StringComparison.Ordinal);
        Assert.Contains("Base commit: b60fb382", notes, StringComparison.Ordinal);
        Assert.Contains("isolated GreeAliceBridge API", notes, StringComparison.Ordinal);
        Assert.Contains("offline Yandex Smart Home endpoints", notes, StringComparison.Ordinal);
        Assert.Contains("dummy split AC device", notes, StringComparison.Ordinal);
        Assert.Contains("VRF/GMV child-unit model", notes, StringComparison.Ordinal);
        Assert.Contains("localhost-only HTTP smoke boundary", notes, StringComparison.Ordinal);
        Assert.Contains("dry-run fail-closed /action", notes, StringComparison.Ordinal);
        Assert.Contains("real Yandex provider registration", notes, StringComparison.Ordinal);
        Assert.Contains("real OAuth", notes, StringComparison.Ordinal);
        Assert.Contains("real credentials/tokens", notes, StringComparison.Ordinal);
        Assert.Contains("production endpoint", notes, StringComparison.Ordinal);
        Assert.Contains("production deployment", notes, StringComparison.Ordinal);
        Assert.Contains("live Gree+ Cloud read-only", notes, StringComparison.Ordinal);
        Assert.Contains("MQTT", notes, StringComparison.Ordinal);
        Assert.Contains("device control", notes, StringComparison.Ordinal);
        Assert.Contains("not usable by real Yandex app as production provider", notes, StringComparison.Ordinal);
        Assert.Contains("## Included capabilities", notes, StringComparison.Ordinal);
        Assert.Contains("## Excluded capabilities", notes, StringComparison.Ordinal);
        Assert.Contains("## Known limitations", notes, StringComparison.Ordinal);
        Assert.Contains("## Safety boundaries", notes, StringComparison.Ordinal);
        Assert.Contains("## How to run local smoke", notes, StringComparison.Ordinal);
    }

    [Fact]
    public void AuditAndReadmePointToRc1AndPilotTrackWithoutProductionReadiness()
    {
        string audit = ReadRepoFile("docs", "integrations", "gree-alice", "release-readiness-audit.md");
        string readme = ReadRepoFile("docs", "integrations", "gree-alice", "README.md");

        Assert.Contains("Current Yandex Smart Home production release status: NOT READY.", audit, StringComparison.Ordinal);
        Assert.Contains("## RC1 decision", audit, StringComparison.Ordinal);
        Assert.Contains("GREE-ALICE-RC1", audit, StringComparison.Ordinal);
        Assert.Contains("GREE-ALICE-PILOT-1", audit, StringComparison.Ordinal);
        Assert.Contains("Current internal/offline status: GREE-ALICE-RC1 candidate.", readme, StringComparison.Ordinal);
        Assert.Contains("Current production Yandex status: NOT READY.", readme, StringComparison.Ordinal);
        Assert.Contains("Next real pilot track: GREE-ALICE-PILOT-1.", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void ExistingReleaseRelevantBoundariesRemainCompatible()
    {
        string script = ReadRepoFile("scripts", "integrations", "gree-alice", "run-local-yandex-provider-smoke.ps1");

        Assert.Contains("RepoRoot", script, StringComparison.Ordinal);
        Assert.Contains("SkipRestore", script, StringComparison.Ordinal);
        Assert.Contains("SkipBuild", script, StringComparison.Ordinal);
        Assert.Equal("localhost-only", GreeAliceLocalHttpSmokeBoundary.HttpSmokeMode);
        Assert.False(GreeAliceLocalHttpSmokeBoundary.AllowedPublicHosts);
        Assert.Equal("not-ready", GreeAliceYandexProviderReadinessBoundary.ProviderReadinessStatus);
        Assert.False(GreeAliceMinimalProductionPilotBoundary.ProductionPilotApproved);
        Assert.True(GreeAliceLocalHttpSmokeBoundary.RequiresFailClosedActions);
    }

    private static void AssertRepoFileExists(params string[] relativeParts)
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(new[] { root }.Concat(relativeParts).ToArray());

        Assert.True(File.Exists(path), "Expected repository file to exist: " + path);
    }

    private static string ReadRepoFile(params string[] relativeParts)
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(new[] { root }.Concat(relativeParts).ToArray());

        Assert.True(File.Exists(path), "Expected repository file to exist: " + path);

        return File.ReadAllText(path);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AssistantEngineer.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate AssistantEngineer.sln from " + AppContext.BaseDirectory);
    }
}
