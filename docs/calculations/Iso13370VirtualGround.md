# ISO13370-Style Virtual Ground Calculation

## Scope

- ISO13370-style virtual ground calculation lane for slab-on-ground style coupling inputs.
- Internal analytical anchor implementation in C#.
- Monthly virtual ground temperature and monthly equivalent ground heat-transfer coefficient outputs.
- Additive lane beside existing simplified ground boundary model.

## Supported Calculation Inputs

- characteristic floor dimension from floor area and exposed perimeter;
- slab/floor thermal resistance;
- ground conductivity;
- equivalent ground thickness (explicit or auto-derived);
- annual-average outdoor temperature;
- monthly outdoor temperature profile (12 values) or seasonal synthetic monthly profile;
- seasonal amplitude and phase;
- optional perimeter thermal bridge contribution.

## Formula-Level Structure

1. `B' = FloorArea / (0.5 * ExposedPerimeter)`
2. `Rground = EquivalentGroundThickness / GroundConductivity`
3. `Ueq = 1 / (Rslab + Rground)`
4. `Hbase = Ueq * FloorArea`
5. `Hbridge = Psi * Length` when perimeter thermal bridge is enabled.
6. `Hannual = Hbase + Hbridge`
7. Monthly outdoor and virtual-ground seasonal components are built deterministically from annual-average + amplitude/phase inputs.
8. Monthly `H` profile is a deterministic seasonal modulation around `Hannual`.

## Integration Boundary

- Existing `Iso13370GroundHeatTransferService` keeps backward-compatible default behavior.
- Virtual lane is selected only via explicit option.
- Returned `H` remains compatible with existing `GroundBoundaryCondition` usage.
- Virtual hourly boundary profile can be mapped through `GroundBoundaryToIso52016BoundaryProfileMapper` by using the 8760-expanded virtual profile.

## Assumptions

- Internal engineering assumptions for seasonal smoothing/attenuation.
- Monthly-to-hourly expansion uses deterministic non-leap month lengths (8760).
- This lane targets controlled engineering use, not certification usage.

## Limitations

- Not full ISO 13370 compliance.
- No full transient soil model claim.
- No full moisture/latent soil coupling claim.
- No detailed foundation edge multidimensional model claim.
- No full validation claim.
- No external validation claim.
- No StandardReference comparison claim.
- No EnergyPlus comparison workflow claim.

## Fixtures

- `tests/fixtures/ground/iso13370/slab-on-ground-basic.json`
- `tests/fixtures/ground/iso13370/insulated-slab.json`
- `tests/fixtures/ground/iso13370/high-conductivity-ground.json`
- `tests/fixtures/ground/iso13370/thermal-bridge-enabled.json`

