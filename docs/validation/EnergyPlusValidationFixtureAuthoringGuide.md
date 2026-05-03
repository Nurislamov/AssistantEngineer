# EnergyPlus Validation Fixture Authoring Guide

Use case-metadata.json, assistantengineer-input.json, comparison-tolerances.json and reference-output.placeholder.json or energyplus-output.reference.json.

PlaceholderComparison is not real EnergyPlus validation.

EnergyPlus fixture authoring template marker
docs/validation/fixtures/_template

## Scaffold command marker

Use the scaffold wrapper when creating a new validation fixture:

.\scripts\engineering-core\new-energyplus-validation-fixture.ps1

The authoring flow still starts from docs/validation/fixtures/_template, requires registry update, local generation, and future real EnergyPlus reference provenance.
PlaceholderComparison is not real EnergyPlus validation, and future real validation must remain tolerance-based.


## Fixture authoring guard markers

Required generated fixture files
Required registry update
Required local generation
Future real EnergyPlus reference
provenance.template.json
energyplus-output.reference.template.json
PlaceholderComparison is not real EnergyPlus validation
future real validation must remain tolerance-based
EnergyPlusValidationFixtureAuthoringKitTests
