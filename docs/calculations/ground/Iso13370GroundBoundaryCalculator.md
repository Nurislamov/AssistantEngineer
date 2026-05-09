# AE-GROUND-001 ISO13370-inspired virtual ground boundary depth

## Stage

- Stage id: `AE-GROUND-001`
- Scope: pure C# ground boundary calculator with virtual monthly boundary temperature profile.

## Claim boundary

- ISO13370-inspired ground boundary engineering calculator.
- Internal deterministic engineering anchors only.
- No full ISO 13370 compliance claim.
- No StandardReference equivalence claim.
- No EnergyPlus comparison workflow claim.
- No ASHRAE 140 / BESTEST-style validation anchor claim.
- No external certification claim.

## Formula structure

1. Characteristic dimension:
   - `B' = AreaM2 / (0.5 * ExposedPerimeterM)`.
2. Equivalent ground U-value:
   - base term: `GroundConductivity / (B' + BaseCharacteristicDepth)`;
   - perimeter amplification;
   - burial depth and below-grade height factor;
   - insulation reduction factor;
   - contact-kind factor;
   - crawlspace ventilation modifier.
3. Blended equivalent U:
   - internal deterministic blend of geometric equivalent U and floor U-value.
4. Heat transfer coefficient:
   - `HeatTransferCoefficientWPerK = EquivalentGroundUValueWPerM2K * AreaM2`.
5. Boundary weights:
   - `GroundWeight`, `OutdoorWeight`, `IndoorWeight` based on contact kind and ventilation rate.
6. Monthly boundary temperatures:
   - ground monthly profile from annual mean + sinusoidal amplitude/phase;
   - outdoor monthly profile from fixture input or annual-mean fallback;
   - weighted blend:
     `Tboundary = GroundWeight * Tground + OutdoorWeight * Toutdoor + IndoorWeight * TindoorAnnualMean`.

## Diagnostics

- clamps for non-physical or zero/negative area, perimeter, conductivity, and floor U-value;
- summary diagnostic with contact kind, characteristic dimension, equivalent U, and heat transfer value.

## Deterministic fixtures

Fixtures for this stage live in:

- `tests/fixtures/ground/iso13370/slab-on-ground-simple.json`
- `tests/fixtures/ground/iso13370/conditioned-basement-buried.json`
- `tests/fixtures/ground/iso13370/unconditioned-basement-outdoor-coupled.json`
- `tests/fixtures/ground/iso13370/ventilated-crawlspace-outdoor-dominant.json`

## Limitations

- internal engineering anchor model;
- not a full ISO 13370 compliance engine;
- not a certification artifact;
- no external equivalence claims.

## Migration strategy

- existing `Iso13370GroundHeatTransferService` remains the compatibility path;
- new calculator and adapter are additive for controlled adoption in later opt-in stages.
- deeper slab-on-ground virtual temperature lane is documented in `docs/calculations/Iso13370VirtualGround.md`.
