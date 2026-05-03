# EnergyPlus Validation Fixture Catalog

Run generate-energyplus-validation-fixture-catalog.ps1.

Checks registry cases without fixture folders, fixture folders without registry entries, fixtures missing required files and fixtures missing comparison output.

Required files:
- case-metadata.json
- assistantengineer-input.json
- comparison-tolerances.json
- reference-output.placeholder.json or energyplus-output.reference.json

Guarded by EnergyPlusValidationFixtureCatalogTests.
