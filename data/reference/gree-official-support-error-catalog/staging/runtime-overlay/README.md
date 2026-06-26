# Gree GMV runtime overlay staging

This folder contains a tracked staging overlay preview for the approved priority Gree GMV reference catalog.

Current scope:

- Stage: ED-24GEC.5D
- Approved priority entries: 17
- Runtime target entries: 17
- Blocked mappings: 1
- Runtime status: disabled
- Telegram bot status: not connected

This is not the runtime diagnostics catalog. It is a reviewable staging layer that maps approved reference data to existing runtime JSON targets.

Safety rules:

- Do not modify `data/equipment-diagnostics/error-knowledge` in this stage.
- `runtimeEnabled` must remain `false`.
- `diagnosticsRuntimeEnabled` must remain `false`.
- `C0` must map to `gree/gmv6/debugging/c0.json` for this GMV overlay.
- `gree/gmv-mini/indoor/c0.json` must remain blocked from this GMV overlay.
- Runtime importer must be a separate stage with tests.

Validation:

```powershell
powershell -ExecutionPolicy Bypass -File ".\tools\gree-support\validate-gree-gmv-review-catalog.ps1" -RepoRoot "D:\Project\AssistantEngineer" -StrictDraftText
powershell -ExecutionPolicy Bypass -File ".\tools\gree-support\validate-gree-gmv-approved-priority-catalog.ps1" -RepoRoot "D:\Project\AssistantEngineer"
powershell -ExecutionPolicy Bypass -File ".\tools\gree-support\validate-gree-gmv-runtime-overlay-staging.ps1" -RepoRoot "D:\Project\AssistantEngineer"
```