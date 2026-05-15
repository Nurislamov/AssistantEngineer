# External Numerical Validation Roadmap

## Purpose

Define a staged external numerical validation backlog for AssistantEngineer without overclaiming compliance, parity, or certification.

This roadmap does not change runtime calculations and does not change expected numerical values.

## Claim Boundaries

- No ASHRAE 140 compliance claim.
- No exact EnergyPlus equivalence claim.
- No third-party calculator-authoritative truth claim.
- No full ISO compliance claim.
- No certified/certification claim for current releases.

## Tier Model

### Tier 0: Internal Deterministic Anchors

- Scope: deterministic internal fixtures and regression anchors already used in engineering gates.
- Required evidence:
  - fixture inputs and expected outputs in versioned repository artifacts;
  - deterministic rerun proof in CI/local regression logs;
  - explicit diagnostics/non-claims attached to outputs.
- Acceptable tolerance policy:
  - exact match where outputs are discrete/structural;
  - existing numeric tolerances where already defined by current fixtures.
- Non-claims:
  - does not claim external standard validation;
  - does not claim external tool equivalence.
- Artifact naming:
  - `INT-DET-<domain>-<case-id>` for case ids;
  - `docs/reports/validation/INT-DET-<domain>-<case-id>-<timestamp>.{md,json}`.

### Tier 1: Independent Manual Engineering Cases

- Scope: hand-derived/reference arithmetic checks independent from production code path.
- Required evidence:
  - derivation sheet or markdown with formulas and units;
  - independent reviewer sign-off (engineering peer review note);
  - mapped input/output traceability to fixture id.
- Acceptable tolerance policy:
  - strict absolute/relative tolerance per metric, declared per case;
  - tolerance rationale recorded in case metadata.
- Non-claims:
  - does not claim ASHRAE 140 / BESTEST-style validation coverage;
  - does not claim full ISO compliance.
- Artifact naming:
  - `MAN-ENG-<domain>-<case-id>`;
  - `tests/fixtures/validation/manual/<case-id>/` with `case-metadata.json`, `derivation.md`, `comparison-tolerances.json`.

Current Tier 1 fixture baseline:

- `MAN-ENG-HEAT-001` steady-state single room heating loss, fixture directory `tests/fixtures/validation/manual/MAN-ENG-HEAT-001-steady-state-room-loss/`.
- `MAN-ENG-VENT-001` ventilation/infiltration sensible heating load, fixture directory `tests/fixtures/validation/manual/MAN-ENG-VENT-001-ventilation-infiltration-sensible-load/`.
- `MAN-ENG-SOLAR-001` simple window solar heat gain, fixture directory `tests/fixtures/validation/manual/MAN-ENG-SOLAR-001-simple-window-solar-gain/`.
- `MAN-ENG-GROUND-001` simple ground boundary steady heat loss, fixture directory `tests/fixtures/validation/manual/MAN-ENG-GROUND-001-simple-ground-boundary-loss/`.
- `MAN-ENG-DHW-001` simple domestic hot water demand, fixture directory `tests/fixtures/validation/manual/MAN-ENG-DHW-001-simple-domestic-hot-water-demand/`.
- `MAN-ENG-SYS-001` simple useful-to-final system energy chain, fixture directory `tests/fixtures/validation/manual/MAN-ENG-SYS-001-useful-to-final-energy-chain/`.

### Tier 2: Published Benchmark-Style Fixtures

- Scope: publicly documented benchmark-style scenarios modeled as reproducible fixtures.
- Required evidence:
  - published source reference citation and version/date;
  - fixture provenance file with normalization assumptions;
  - reproducible runner output and disclosure section.
- Acceptable tolerance policy:
  - metric-level tolerance table (absolute/relative) with rationale;
  - no implicit zero-tolerance unless explicitly justified.
- Non-claims:
  - does not claim certification-level validation;
  - does not claim exact solver-to-solver equivalence.
- Artifact naming:
  - `PUB-BMK-<suite>-<case-id>`;
  - `docs/validation/fixtures/<suite>/<case-id>/` plus `provenance.json` and `comparison-tolerances.json`.

### Tier 3: External Tool Comparison (Diagnostic Only)

- Scope: comparison against external tools (including EnergyPlus) for diagnostic triangulation only.
- Required evidence:
  - tool version/build and run configuration;
  - exported source artifacts (model/weather/output) with checksums;
  - mismatch analysis notes and follow-up classification.
- Acceptable tolerance policy:
  - tolerance-based comparison only;
  - trend/deviation thresholds documented per metric and scenario.
- Non-claims:
  - does not claim exact EnergyPlus equivalence;
  - does not claim ASHRAE 140 / BESTEST-style validation coverage;
  - does not treat third-party calculator outputs as authoritative truth.
- Artifact naming:
  - `EXT-DIAG-<tool>-<case-id>`;
  - `tests/fixtures/validation/<tool>/<case-id>/` and `docs/reports/validation/<case-id>-ComparisonResult.{md,json}`.

### Tier 4: Certification-Level Validation (Future Only)

- Scope: future formal program participation/certification workflows (not part of current release gates).
- Required evidence:
  - formal protocol package and externally auditable run logs;
  - independent assessment/report package;
  - governance approval record to promote claim language.
- Acceptable tolerance policy:
  - protocol-defined tolerance rules only;
  - deviation handling and waiver process documented.
- Non-claims:
  - current project state is not certified;
  - tier remains future/backlog until governance promotion criteria are met.
- Artifact naming:
  - `CERT-FUTURE-<program>-<milestone-id>`;
  - `docs/validation/certification/<program>/<milestone-id>/`.

## Entry/Exit Gates

- Tier promotion requires all required evidence in current tier plus non-claims retained in generated docs/reports.
- Any positive claim language requires explicit governance approval and dedicated fixture/report evidence.
- Failing claim-boundary scanner blocks promotion artifacts from being marked release-ready.

## Backlog Priorities

1. Expand Tier 1 manual independent cases for room/floor/building load and annual balance baselines.
2. Promote selected Tier 1 cases into Tier 2 published benchmark-style fixtures with provenance metadata.
3. Strengthen Tier 3 diagnostic tooling coverage for external comparison deltas and drift triage.
4. Define Tier 4 governance checklist template (future-only, no release claims).

## Explicit Non-Claims for This Roadmap

- Does not claim exact EnergyPlus equivalence.
- Does not claim ASHRAE 140 / BESTEST-style validation coverage.
- Does not claim full ISO compliance.
- Does not claim certified status.
