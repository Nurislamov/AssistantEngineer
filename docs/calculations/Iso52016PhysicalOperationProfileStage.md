# AE-ISO52016-002 Step 04 - physical operation profile stage

This stage extends the ISO52016-inspired physical room/zone model builder with optional hourly operation profiles.

## Scope

Step 04 keeps the existing Matrix solver and the previous physical node/surface/boundary stages. It adds two small application-owned contracts:

- `Iso52016PhysicalHourlyOperationCondition`
- `Iso52016MatrixHourlyBoundaryConductanceOverride`

The Matrix request previously had static boundary conductance links. Step 04 adds optional per-hour overrides for declared boundary links. This is required for physically honest variable ventilation/infiltration support: changing only a boundary temperature is not equivalent to changing the ventilation heat transfer coefficient.

## Builder behaviour

The physical builder can now map per-hour operation inputs into the Matrix request:

- ventilation/infiltration heat transfer coefficient;
- ventilation boundary temperature, such as outdoor, supply, or preconditioned air temperature;
- internal gains convective fraction;
- direct solar gains to air fraction.

Values not provided in `OperationConditions` fall back to the existing hourly input profile and model options. Without `OperationConditions`, previous Step 03 behaviour is preserved.

For ventilation, the builder declares one ventilation boundary link when at least one hour has positive ventilation conductance. Each hour then gets an explicit `BoundaryConductanceOverride`, including zero where ventilation is intentionally off.

## Validation anchors

The stage rejects:

- duplicate operation rows for the same hour;
- operation rows referencing hours that are not in the hourly profile;
- negative or non-finite ventilation heat transfer coefficients;
- non-finite ventilation boundary temperatures;
- internal gains convective fractions outside `[0, 1]`;
- solar gains to air fractions outside `[0, 1]`;
- Matrix boundary conductance overrides that do not match a declared boundary link.

## Claim boundary

This is an ISO52016-inspired physical operation profile stage with validation/internal engineering anchors only.

It is not complete ISO 52016 numerical equivalence, not StandardReference numerical equivalence, not EnergyPlus numerical equivalence, and not ASHRAE Standard 140 benchmark-grade claim.

No generated artifacts are introduced by this step.
