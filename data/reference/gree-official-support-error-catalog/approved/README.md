# Gree GMV approved reference catalog

This folder contains approved reference data for a limited priority subset of Gree GMV error-code cards.

Current scope:

- Brand: Gree
- Family/model: GMV
- Approved priority entries: 17
- Runtime status: disabled
- Telegram bot status: not connected

The files here are reference data only. They must not be used as direct runtime diagnostics without a separate importer, tests, and explicit mapping into the application diagnostics catalog.

Safety rules:

- `runtimeEnabled` must remain `false`.
- `diagnosticsRuntimeEnabled` must remain `false`.
- Source references must point back to `review/` and `raw/cards/`.
- Do not bulk-import all 256 raw/review records into runtime.
- Keep visually similar codes distinct, especially `o1`, `01`, `O1`, `Ho`, `H0`, `No`, and `N0`.

Validation:

```powershell
powershell -ExecutionPolicy Bypass -File ".\tools\gree-support\validate-gree-gmv-review-catalog.ps1" -RepoRoot "D:\Project\AssistantEngineer" -StrictDraftText

powershell -ExecutionPolicy Bypass -File ".\tools\gree-support\validate-gree-gmv-approved-priority-catalog.ps1" -RepoRoot "D:\Project\AssistantEngineer"
```