# Error knowledge JSON

This directory is the source of truth for localized, audience-aware error knowledge.

Use one entry per file:

```text
{manufacturer}/{series}/{code}.json
```

Every entry includes product/signal/display/system taxonomy and a `packageId`. Package manifests live in
`packages/{packageId}.json` and define source/review context plus the classifications and expected entry count allowed
for that batch.

The current audiences are `Consumer`, `Installer`, and `Engineer`; Owner/Admin reuse Engineer output. Valid locale
keys are `ru`, `en`, and future-ready `uz`; Uzbek content is not required or exposed yet.

Validate every change:

```powershell
dotnet run --project tools/AssistantEngineer.Tools.EquipmentDiagnosticsVerification -- verify-knowledge
```

Review guidance and the full field convention are documented in
`docs/equipment-diagnostics/error-knowledge-v2-localization.md`.
