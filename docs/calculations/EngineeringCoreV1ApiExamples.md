# Engineering Core V1 API Examples

## Engineering core status endpoint

Engineering Core V1 status is exposed through the calculations API.

    GET /api/v1/calculations/engineering-core/v1/status

Expected status response shape:

    {
      "coreName": "AssistantEngineer Engineering Core",
      "version": "v1",
      "status": "ClosedV1",
      "formulaGatesClosed": true,
      "weather8760GatesClosed": true,
      "annualHourly8760GateClosed": true,
      "successfulResultsMustNotContainErrorDiagnostics": true,
      "formulaGates": [
        {
          "calculationId": "HVAC.TRANSMISSION.SIMPLE_UA",
          "name": "Transmission heat transfer",
          "status": "ClosedV1",
          "priority": "P0",
          "scope": "Steady-state component heat transfer.",
          "limitation": "Does not claim full dynamic ISO 52016 node/matrix heat-balance parity."
        }
      ],
      "explicitNonClaims": [
        "No exact pyBuildingEnergy numerical parity claim.",
        "No exact EnergyPlus numerical parity claim.",
        "No ASHRAE 140 validation coverage claim."
      ],
      "outOfScopeV1": [
        "HVAC.LATENT_LOAD",
        "HVAC.MOISTURE_BALANCE"
      ],
      "plannedValidation": [
        "VALIDATION.ENERGYPLUS_ASHRAE140"
      ],
      "requiredAnnual8760Flags": [
        "EnergyDataSource = TrueHourlySimulation",
        "IsTrueHourly8760 = true",
        "HourlyRecordCount = 8760"
      ],
      "documentationFiles": [
        "docs/calculations/EngineeringCoreV1Scope.md",
        "docs/calculations/EngineeringCoreV1ReleaseNotes.md",
        "docs/calculations/EnergyPlusAshrae140ValidationPlan.md"
      ]
    }

## How to interpret status

ClosedV1 means:

- the formula gate is implemented;
- units are documented;
- diagnostics are available;
- deterministic tests cover the calculation path;
- invalid mandatory inputs fail the calculation;
- known limitations are explicitly documented.

ClosedV1 does not mean:

- exact EnergyPlus parity;
- exact pyBuildingEnergy parity;
- ASHRAE 140 validation coverage;
- full ISO 52016 node/matrix solver implementation;
- full detailed HVAC plant simulation.

## Report disclosures

Heating and cooling report contracts expose calculationDisclosure.

Example:

    {
      "projectName": "Demo Project",
      "buildingName": "Office Building",
      "calculationMethod": "EngineeringCoreV1.DesignPointCooling",
      "calculationDisclosure": {
        "coreStatus": "ClosedV1",
        "calculationScope": "Engineering-core v1 cooling design-point report.",
        "calculationMethod": "EngineeringCoreV1.DesignPointCooling",
        "actualMethod": "EngineeringCoreV1.DesignPointCooling",
        "warnings": [
          "Cooling report uses engineering design-point load calculation.",
          "Report does not claim full ISO 52016 node/matrix solver parity.",
          "Report does not claim exact EnergyPlus, ASHRAE 140 or pyBuildingEnergy numerical parity.",
          "Latent load, moisture balance and detailed psychrometrics are out of scope for engineering-core v1."
        ],
        "assumptions": [
          "Cooling load is assembled from transmission, ventilation, infiltration, solar and internal gain components.",
          "Window solar gains use simplified SHGC/shading based engineering model.",
          "Surface irradiance uses ISO52010-inspired solar geometry and isotropic sky transposition."
        ],
        "explicitNonClaims": [
          "No exact EnergyPlus numerical parity claim.",
          "No ASHRAE 140 validation coverage claim.",
          "No full ISO 52016 node/matrix solver parity claim."
        ],
        "outOfScopeV1": [
          "HVAC.LATENT_LOAD",
          "HVAC.MOISTURE_BALANCE"
        ],
        "documentationFiles": [
          "docs/calculations/EngineeringCoreV1Scope.md",
          "docs/calculations/EngineeringCoreV1ReleaseNotes.md",
          "docs/calculations/EnergyPlusAshrae140ValidationPlan.md"
        ]
      }
    }

## Annual energy 8760 rule

Annual energy may be presented as true hourly 8760 only when all three conditions are true:

    EnergyDataSource = TrueHourlySimulation
    IsTrueHourly8760 = true
    HourlyRecordCount = 8760

If any condition is missing, the result may still be useful as deterministic/diagnostic output, but it must not be described as true annual 8760 simulation.

## Diagnostics rule

Application and report consumers must interpret diagnostics as:

| Severity | Meaning |
|---|---|
| Error | Invalid mandatory input. Calculation must fail. |
| Warning | Fallback, simplified assumption, missing optional assumption or partial source. |
| Info | Method/source/metadata diagnostic. |

A successful calculation result must not contain CalculationDiagnosticSeverity.Error.

## Frontend display recommendation

For calculation results and reports, the frontend should display:

1. main result values;
2. calculation method / actual method;
3. warnings;
4. assumptions;
5. explicit non-claims;
6. out-of-scope v1 items;
7. documentation links.

The warnings and non-claims must not be hidden behind debug-only UI.
