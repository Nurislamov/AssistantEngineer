# P3-13 - Building Input Validation hotspot refactor

## Status

Implemented.

## Target hotspot

- `BuildingInputValidationService.cs`
- Line count before refactor: 784
- Line count after refactor: 67

## Done

- `BuildingInputValidationService` is now a focused facade/orchestrator.
- Room/floor geometry validation was extracted.
- Envelope/opening validation was extracted.
- Ventilation validation was extracted.
- Ground/boundary/construction validation was extracted.
- DHW/system-energy/ISO52016 readiness validation was extracted.
- Shared diagnostic/result factory helpers were extracted.
- Existing validation logic was moved without intentional semantic changes.

## Preserved behavior

- No calculation physics changes.
- No public API route changes.
- No public DTO redesign.
- No intentional diagnostic code/severity/message/order changes.

## Explicitly out of scope

- New validation standard coverage.
- New engineering assumptions.
- Frontend diagnostic redesign.
- Broad validation architecture rewrite.

## Verification

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\engineering-core\verify-p3-13-building-input-validation-refactor.ps1
```

