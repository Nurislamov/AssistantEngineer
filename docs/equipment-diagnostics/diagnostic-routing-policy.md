# Diagnostic Routing Policy

ED-24UX.1 defines deterministic routing for Telegram and bot diagnostic lookups when the same displayed code exists in more than one Gree VRF context.

## Inputs

The routing layer uses only explicit request data:

- raw Telegram text;
- parsed diagnostic code with canonical casing preserved from the selected knowledge entry;
- manufacturer;
- optional series/model hints;
- optional equipment side and display-surface hints;
- candidate metadata from reviewed runtime or repository-backed diagnostic knowledge.

It must not infer diagnostic meaning from text similarity, source-reference text, Telegram captions, manual filenames, internet data, or model memory.

## Series Hints

The parser recognizes multi-word Gree VRF hints, including:

- `GMV6`
- `GMV Mini`, `GMV-Mini`, `GMV5 Mini`, `Mini`
- `GMV Slim`, `GMV5 Slim`
- `GMV X`
- `GMV X PRO`

Examples that must be accepted:

- `Gree GMV Mini AJ`
- `Gree GMV Mini C0`
- `GMV Mini AJ`
- `GMV Mini C0`

Numeric `01` remains a numeric token and must not match lowercase `o1`.

## Candidate Outcomes

The candidate resolver has three outcomes:

- `DirectAnswer`: exactly one candidate remains, or all remaining candidates share one explicit `meaningGroupId`;
- `ClarificationRequired`: more than one different meaning remains;
- `NotFound`: no reviewed candidate remains.

Clarification order is brand, equipment group, series, display surface, then exact candidate.

## Same-Meaning Groups

`meaningGroupId` is optional metadata on repository-backed error-knowledge entries. It is explicit only; no text similarity or source-reference overlap creates a group.

`sourceReferences[]` continues to mean reviewed source evidence for the same answer. It is not a routing rule by itself.

When all remaining candidates share one `meaningGroupId`, Telegram returns one answer and lists the applicable contexts. ED-24UX.1 marks Gree VRF `C0` communication malfunction for GMV6 and GMV Mini as:

`gree-vrf-gmv-communication-c0`

Therefore:

- `Gree C0` returns one grouped answer with applicable contexts `Gree GMV6` and `Gree GMV Mini`;
- `Gree GMV6 C0` resolves GMV6;
- `Gree GMV Mini C0` resolves GMV Mini.

Grouped same-meaning output must remain neutral:

- its title must not present one candidate series as the only result;
- its next step must point to the manual for the applicable/known series instead of forcing the representative candidate's manual;
- it may list reviewed applicability contexts;
- it must not expose `meaningGroupId`, package/source identifiers, local paths, or Telegram manual file identifiers.

Explicit series requests remain series-specific.

## Visually Confusable Codes

Lookup does not silently substitute visually similar characters. Numeric `01` is not lowercase `o1`.

When a rejected code has a known visually confusable reviewed alternative, Telegram may explain the distinction and invite an explicit lookup. The suggestion is presentation guidance only and must not store or resolve the alternative until the user explicitly requests it.

Reviewed answers may include compact notes for ambiguous canonical codes, for example:

- `o1` is letter O plus digit 1;
- `L1` is letter L plus digit 1.

See [Diagnostic Answer Quality Baseline](diagnostic-answer-quality-baseline.md) for answer-class and wording rules.

## Runtime Boundary

Runtime catalog answers remain authoritative for existing runtime scenarios unless a precise series hint or an explicit same-meaning group requires repository-backed routing. This preserves existing GMV6 smoke behavior while allowing reviewed GMV Mini manual-backed entries to resolve.
