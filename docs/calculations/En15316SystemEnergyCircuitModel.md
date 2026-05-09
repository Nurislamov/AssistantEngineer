# EN15316-Style Heating Circuit Foundation

## Scope

This document describes the additive EN15316-style system energy calculation foundation for circuit-level heating representation in AssistantEngineer.

The foundation is implemented as an internal engineering anchor and does not replace the existing simplified EN15316-inspired system-energy chain.

## Supported Circuit Foundation

- heating circuit identity and type
- emission system model abstraction with model-kind enum (`Simplified`, `C2`, `C3`, `C4`, `C5`)
- distribution, storage, and generation model containers
- flow/return temperature pair contracts
- deterministic time-step input/result contracts
- input validator for circuit consistency and numeric constraints
- stage-by-stage deterministic energy chain:
  - emission
  - distribution
  - storage (optional/pass-through if not provided)
  - generation
  - final energy and primary energy

At this stage:

- `Simplified` and `C3` emission-model branches are explicitly implemented in the circuit calculator;
- `C2`, `C4`, and `C5` are represented by contracts and deterministic fallback behavior.

## Relationship To Existing Simplified Chain

- existing `En15316SystemEnergyChainCalculator` behavior remains unchanged;
- existing `SystemEnergyEngine` opt-in behavior remains unchanged;
- circuit-level contracts/services are additive and are not wired as the default production path.
- existing simplified path remains default unless explicit opt-in is enabled.
- pipeline useful-energy handoff into this circuit path is additive and opt-in only.

## Calculation Chain And Formulas

For each timestep and circuit:

1. useful demand:
   - `usefulTotal = usefulHeating + usefulDhw`
2. emission stage:
   - `emissionInput = emissionOutput / emissionEfficiency` or `emissionOutput * (1 + emissionLossFactor)`
   - `emissionLoss = emissionInput - emissionOutput`
3. distribution stage:
   - `distributionInput = distributionOutput / distributionEfficiency` or `distributionOutput * (1 + distributionLossFactor)`
   - `distributionLoss = distributionInput - distributionOutput`
4. storage stage (optional):
   - pass-through when no storage penalty is configured
   - otherwise same efficiency/loss-factor form
5. generation stage:
   - boiler/generic efficiency: `finalGeneration = generatorOutput / efficiency`
   - heat pump COP: `finalGeneration = generatorOutput / COP`
   - electric resistance: deterministic efficiency path (or pass-through fallback)
6. auxiliary and primary:
   - `finalEnergy = finalGeneration + auxiliaryFraction * finalGeneration`
   - `primaryEnergy = finalEnergy * primaryEnergyFactor`

Monthly and annual results are deterministic sums of timestep outputs.

## Temperature Handling

- fixed flow/return operating condition is supported;
- per-timestep outdoor temperature override is supported;
- optional outdoor-reset placeholder is supported with deterministic slope/reference parameters.

## Examples / Fixtures

- `tests/fixtures/system-energy/en15316/boiler-simple-circuit.json`
- `tests/fixtures/system-energy/en15316/heat-pump-simple-circuit.json`
- `tests/fixtures/system-energy/en15316/distribution-losses-enabled.json`
- `tests/fixtures/system-energy/en15316/zero-demand.json`

## Pipeline Handoff Integration

- building useful heating/cooling energy can be mapped from pipeline annual-energy output into system-energy handoff contracts;
- optional DHW useful-energy handoff can be merged into the same deterministic input package;
- the circuit-level calculator consumes heating and DHW timestep inputs, while cooling useful energy remains preserved in handoff metadata and is evaluated through the existing deterministic cooling conversion path in `SystemEnergyEngine`.

This is an internal engineering anchor for staged integration. It does not claim full EN15316 compliance and does not claim external validation.

## Limitations

- no full EN15316 compliance claim;
- no external validation claim;
- no manufacturer performance claim;
- no full HVAC control-loop simulation;
- no hydronic transient model;
- no certification-grade compliance output;
- no national annex completeness claim in this stage.

## Claim Boundary

- EN15316-style system energy calculation.
- Internal deterministic engineering anchor.
- Not full validation.
