# ADR 0001: Engineering Core V1 Closure Policy

## Status

Accepted

## Context

AssistantEngineer needs a clear definition of when Engineering Core V1 is closed.

The goal is to deliver a useful engineering calculation kernel for HVAC loads, weather-driven annual energy, domestic hot water, simplified system energy and equipment sizing.

The project uses ISO/pyBuildingEnergy as source structure and formula inspiration, but does not attempt to be an exact clone of pyBuildingEnergy, EnergyPlus or ASHRAE 140.

## Decision

Engineering Core V1 is considered closed as an engineering formula gate when:

- FormulaAuditMatrix has no Partial formula items;
- P0 and P1 formula gates are ClosedV1;
- weather EPW/PVGIS import gates normalize to 8760;
- annual energy has a true hourly 8760 scenario;
- validation flow fails on Error diagnostics;
- reports expose calculationDisclosure;
- frontend shows status/disclosure outside debug-only UI;
- diagnostics catalog exists with userMessage and userAction;
- release manifest matches FormulaAuditMatrix and EngineeringCoreStatusFacade;
- verification scripts and CI guard the closure.

## Consequences

Allowed claims:

- Engineering Core V1 formula gates are closed.
- EPW/PVGIS weather gates normalize to 8760 records.
- Annual hourly energy has a true 8760 scenario.
- Simplified ISO/EN-inspired modules are documented with limitations.

Forbidden claims:

- exact EnergyPlus numerical parity;
- exact pyBuildingEnergy numerical parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver parity;
- full ISO 13370 implementation;
- full EN 15316 implementation;
- latent/moisture/humidity support in v1.

## Verification

Closure is protected by:

    .\scripts\engineering-core\verify-engineering-core-v1.ps1
    .\scripts\engineering-core\verify-engineering-core-v1-manifest.ps1
    .\scripts\engineering-core\generate-engineering-core-v1-release-evidence.ps1

## Future decisions

Future ADRs should cover:

- first real EnergyPlus fixture acceptance policy;
- ASHRAE 140-style case tolerance policy;
- latent/moisture psychrometrics scope;
- equipment part-load performance modeling.
