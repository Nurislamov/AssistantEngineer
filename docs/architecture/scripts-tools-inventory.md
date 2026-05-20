# Scripts and Tools Inventory (P8-00)

## Purpose

Classify engineering scripts and C# tools by operational role after P5/P6/P7.

## Scope

- script wrappers versus tool commands;
- release-critical scripts;
- generated artifact producers;
- consolidation and deprecation candidates.

## Categories

- `KeepAsWrapper`
- `ConvertToToolCandidate`
- `DeprecatedCandidate`
- `ReleaseGateCritical`
- `GeneratedArtifactProducer`
- `UnknownNeedsReview`

## Classification notes

- Release-ready scripts are critical wrappers and must keep deterministic gate behavior.
- Ownership backfill tooling is critical governance/tooling and not a runtime dependency.
- Wrapper scripts around stable C# tool commands are acceptable when they improve operator UX.
- Duplicate wrappers with near-identical arguments are future consolidation candidates.

## Non-claims

- No runtime behavior change claim.
- No calculation physics change claim.
- No production apply enabled claim.
- No ownership backfill execution claim.
- No full tenant isolation claim.
- No production security certification claim.
