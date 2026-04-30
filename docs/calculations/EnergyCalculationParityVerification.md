# Energy Calculation Parity Verification

Verification is based on internal deterministic fixtures and focused engine tests.

## Current Status

| Function | Status | Evidence |
| --- | --- | --- |
| Transmission Heat Transfer | InternalDeterministicTested | Engine tests and transmission fixtures |
| Window Solar Gains | InternalDeterministicTested | Engine tests and solar fixtures |
| Ventilation and Infiltration Loads | InternalDeterministicTested | Engine tests and ventilation fixtures |
| Internal Gains | InternalDeterministicTested | Engine tests |
| Room Heating Load | InternalDeterministicTested | Room load engine tests and fixtures |
| Room Cooling Load | InternalDeterministicTested | Room load engine tests and fixtures |
| Thermal Zone Aggregation | InternalDeterministicTested | Aggregation engine tests and fixtures |
| Floor Aggregation | InternalDeterministicTested | Aggregation engine tests and fixtures |
| Building Aggregation | InternalDeterministicTested | Aggregation engine tests and fixtures |
| Annual Energy Balance | InternalDeterministicTested | Annual engine tests and fixtures |
| DHW Demand | InternalDeterministicTested | DHW tests and fixtures |
| System Energy | InternalDeterministicTested | System energy tests and fixtures |
| Equipment Sizing Integration | InternalDeterministicTested | Equipment sizing tests and fixtures |

No function is marked ExternalParityCovered in this pass.

## Regression Rules

- Non-pending fixtures must load.
- Deterministic fixtures must pass within tolerance.
- Annual totals must equal monthly sums within tolerance.
- Design building loads must equal sums of unique room loads in design-point mode.
- Diagnostics must exist for fallback or clamped assumptions.
- Room cooling separates solar, internal, transmission and ventilation components.
- Room heating separates transmission, ventilation, infiltration and ground components.
- Equipment sizing explains rejected candidates.

## Known Limits

The current status proves internal deterministic consistency. It does not prove external benchmark parity.
