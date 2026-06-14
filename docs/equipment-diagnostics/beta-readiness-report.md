# EquipmentDiagnostics Closed Beta Readiness Report

ED-20A consolidates repository evidence for a controlled closed beta. It is not a production-readiness certificate and not a public-release claim.

Run `.\scripts\equipment-diagnostics\prepare-beta-readiness-report.ps1 -BaseRef origin/master`.

The command writes ignored local artifacts:

- `artifacts/verification/equipment-diagnostics/beta-readiness-report.json`
- `artifacts/verification/equipment-diagnostics/beta-readiness-summary.md`

`Pass` means a checked repository contract is present. `Warning` records an honest limitation that does not block a controlled closed beta. `Blocker` means beta preparation must stop. `NotApplicable` is reserved for checks outside the current beta shape.

The report uses repository files and disabled-by-default configuration only. It never requires or prints real secrets, a real domain, a VPS, Telegram network access, Docker, or external monitoring.

Known limitations remain explicit: no production deploy, no public beta, no database or audit persistence, no external monitoring, no AI/RAG/vector search, and partial manual-backed coverage. The runtime catalog is the only source for final answers; manual-codebook, staging, and preview data are not final diagnosis.

ED-22A can consume this ED-20A evidence through `prepare-telegram-closed-beta-release-evidence.ps1`; it does not change the readiness meaning or activate Telegram.
