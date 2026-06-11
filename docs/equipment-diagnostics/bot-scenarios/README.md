# Equipment Diagnostics Bot Field Scenarios

This source-controlled pack defines deterministic operator acceptance scenarios for the existing EquipmentDiagnostics bot service, API endpoint, and internal frontend panel.

Scenarios describe current approved runtime behavior. They are not diagnostic knowledge, are not embedded runtime resources, and do not promote staging, codebook, preview, or manual content.

Current scenarios:

- `gree-h5-answer`: exact GMV runtime seed answer with verification, provenance, and safety boundaries.
- `gree-c5-clarification`: historical context-sensitive code; current catalog has one Indoor runtime match, so the honest current expectation is `Answer`.
- `gree-f5-answer-or-not-found`: F5 is not runtime-covered, so the expected result is `NotFound`.
- `gree-a0-reference-only`, `gree-n6-reference-only`, `gree-db-reference-only`: reference-only patterns, never final answers.
- `gree-unknown-not-found`: safe unknown-code fallback.
- `gree-ambiguous-code-clarification`: E1 has multiple runtime contexts and requires clarification.

All scenario outputs are checked for deterministic status, required UI boundaries, unsafe wording, internal paths, and non-runtime artifact exposure.
