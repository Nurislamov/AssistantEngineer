# Full Test Baseline Failures

Inventory date: 2026-06-27 17:30:56 +05:00

Commit: `2c6f7efd05ac2ed21c0b4499a0af800305b6f50d`

Scope: ED-24TD.2 inventory only. No runtime JSON, route, wording, deploy, environment, migration, frontend, Telegram polling, service-request, or phone-flow files were changed.

## ED-24TD.3 Resolution

Resolution date: 2026-06-27

Status: resolved.

Full solution result after ED-24TD.3:

```powershell
dotnet test .\AssistantEngineer.sln --logger "console;verbosity=minimal" --logger "trx;LogFileName=ed-24td3-final.trx"
```

Result: 0 failed, 4828 passed, 0 skipped, 4828 total, duration 2 m 12 s.

TRX: `tests/AssistantEngineer.Tests/TestResults/ed-24td3-final.trx`

The five ED-24TD.2 failures were resolved by refreshing stale test/scenario expectations and by ignoring local `.ae-tools/**` analysis artifacts in the beta-readiness scanner. `PublishedApiAssemblyLoadsEmbeddedGreeH5` still does not hang and the full baseline is green.

## Commands Run

```powershell
dotnet test .\AssistantEngineer.sln --logger "console;verbosity=minimal" --logger "trx;LogFileName=ed-24td2-full-solution.trx"
dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~PublishedApiAssemblyLoadsEmbeddedGreeH5" --logger "console;verbosity=minimal"
dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~ErrorKnowledgeJsonValidationTests" --logger "console;verbosity=minimal"
dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~GreeGmvMiniRouting12_1Tests|FullyQualifiedName~GreeGmvMiniVisibleWording12_2Tests|FullyQualifiedName~GreeGmvMiniManualImport12Tests|FullyQualifiedName~GreeGmv6ManualImport11Tests" --logger "console;verbosity=minimal"
dotnet test .\tests\AssistantEngineer.Tests\AssistantEngineer.Tests.csproj --filter "FullyQualifiedName~EquipmentDiagnosticTelegram" --logger "console;verbosity=minimal"
dotnet run --project tools\AssistantEngineer.Tools.EquipmentDiagnosticsVerification --no-restore -- beta-readiness
```

## Overall Result

The full solution test run completed; it did not hang.

Result: 5 failed, 4823 passed, 0 skipped, 4828 total, duration 2 m 41 s.

TRX: `tests/AssistantEngineer.Tests/TestResults/ed-24td2-full-solution.trx`

`PublishedApiAssemblyLoadsEmbeddedGreeH5` is no longer the hanging root cause. The targeted H5 smoke passed separately: 1 passed, duration 327 ms reported by xUnit.

## Targeted Green Areas

| Area | Result |
|---|---:|
| `PublishedApiAssemblyLoadsEmbeddedGreeH5` | 1 passed |
| `ErrorKnowledgeJsonValidationTests` | 74 passed |
| GMV6/Mini import, routing, visible wording tests | 44 passed |
| `EquipmentDiagnosticTelegram` tests | 396 passed |

## Failure Groups

### Group A: Official support source-reference path expectation is stale

Probable area: EquipmentDiagnostics

Likely source: old baseline expectation after official-support reference path naming changed.

Tests:

| Failed test | File/line | Message |
|---|---|---|
| `AssistantEngineer.Tests.EquipmentDiagnostics.GreeGmvOfficialSupportSourceReferenceTests.RuntimeLoaderIncludesOfficialSupportReferenceForApprovedGmvOverlayTargets` | `tests/AssistantEngineer.Tests/EquipmentDiagnostics/GreeGmvOfficialSupportSourceReferenceTests.cs:68` | `Assert.StartsWith()` failed for `reference.SourceReference`. |

Observed detail: runtime entries still contain the official-support reference and 17 expected GMV6 codes are present, but the actual `sourceReference` path starts with `data/reference/gree-official-support-error-catalog/<localized-approved>/...` while the test expects `data/reference/gree-official-support-error-catalog/approved/...`.

Recommended next action: decide whether the canonical support-catalog path should be ASCII `approved/` or localized; then update either the runtime source references or the test expectation.

### Group B: Scenario/status expectations are stale after GMV runtime expansion

Probable area: EquipmentDiagnostics

Likely source: recent GMV6/GMV Mini runtime additions changed lookup behavior; tests still describe pre-import behavior.

Tests:

| Failed test | File/line | Message |
|---|---|---|
| `AssistantEngineer.Tests.EquipmentDiagnostics.EquipmentDiagnosticBotServiceTests.Gmv6U0FallsBackToManualBackedDebuggingKnowledge(series: null, freeText: null)` | `tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticBotServiceTests.cs:197` | Expected `ReferenceOnly`, actual `ClarificationRequired`. |
| `AssistantEngineer.Tests.EquipmentDiagnostics.EquipmentDiagnosticBotScenarioAcceptanceTests.EveryScenarioMatchesCurrentRuntimeBehaviorAndIsDeterministic` | `tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticBotScenarioAcceptanceTests.cs:45` | Expected `NotFound`, actual `Answer`. |

Observed detail: the U0 no-series request now appears ambiguous because the catalog contains more than one applicable GMV-family runtime context. The scenario mismatch is most likely `gree-f5-answer-or-not-found`: it still says F5 has no approved runtime case, but `data/equipment-diagnostics/error-knowledge/gree/gmv6/outdoor/f5.json` now exists.

Recommended next action: update the acceptance scenario pack and U0 service expectation to the current post-GMV-import behavior, or explicitly tighten request context if old behavior is still desired.

### Group C: Localized H5 wording expectation is stale

Probable area: EquipmentDiagnostics

Likely source: old assertion text after H5 Russian visible wording changed.

Tests:

| Failed test | File/line | Message |
|---|---|---|
| `AssistantEngineer.Tests.EquipmentDiagnostics.ErrorKnowledgeLocalizationFoundationTests.AudienceSpecificRussianTextIsSelected` | `tests/AssistantEngineer.Tests/EquipmentDiagnostics/ErrorKnowledgeLocalizationFoundationTests.cs:38` | Installer summary no longer contains the expected old phrase for inverter fan protection. |

Observed detail: the selected installer summary starts with wording equivalent to "For H5 check power, power module, ..." instead of the older phrase asserted by the test.

Recommended next action: refresh the wording assertion to check stable intent rather than one exact old phrase, or restore the old wording if it was intentionally canonical.

### Group D: Beta-readiness scanner reports a local private PDF under `.ae-tools`

Probable area: EquipmentDiagnostics tooling / local workspace hygiene

Likely source: local ignored workspace content is scanned by `NoForbiddenFiles`, but `.ae-tools/` is not excluded by the readiness generator.

Tests:

| Failed test | File/line | Message |
|---|---|---|
| `AssistantEngineer.Tests.EquipmentDiagnostics.EquipmentDiagnosticsBetaReadinessReportTests.CurrentRepositoryProducesCompleteClosedBetaReportWithoutBlockers` | `tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticsBetaReadinessReportTests.cs:16` | Expected blocker count `0`, actual `1`. |

Observed detail from `beta-readiness`: `Security/no-secret readiness` has one blocker: `Forbidden file candidate exists: .ae-tools/gree-support-private-documents/F3-GMV/F3-GMV-low-voltage-sensor-troubleshooting.pdf`.

Recommended next action: either remove/move the private local PDF from this workspace before full baseline runs, or update the readiness generator to treat `.ae-tools/` like other ignored workspace paths if that directory is intentionally local-only.

## First Root Cause Summary

The full baseline is not blocked by the ED-24TD.1 H5 smoke anymore. The remaining failures are concentrated in EquipmentDiagnostics and split into stale post-GMV-import expectations plus one local beta-readiness scanner blocker. There were no Calculation, Architecture, Frontend, Infrastructure, deployment, migration, Telegram polling, service-request, or phone-flow failures in this run.
