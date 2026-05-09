# Natural Ventilation Engineering Foundation

## Scope

This module provides an internal engineering implementation for deterministic natural ventilation calculations and hourly handoff into zone-load pipelines.

Supported opening/control calculations are implemented in C# contracts and services under `AssistantEngineer.Modules.Calculations`.

## Supported modes

- Single-sided simplified wind/stack airflow path.
- Wind-only, stack-only, and combined wind+stack deterministic paths.
- Schedule/custom prescribed-airflow path for deterministic hourly lane construction.
- Closed-opening / no-opening zero-flow path.

## Opening geometry and metadata

Supported opening metadata includes:

- opening id, zone id, and optional room id;
- boundary/surface reference id;
- opening type (`Window`, `Door`, `Louver`/`Louvre`, `Vent`, `Generic`);
- opening area, optional gross/effective area hints;
- opening width/height/center/bottom/top/sill heights;
- discharge coefficient and wind pressure coefficients;
- orientation azimuth;
- operable flag and maximum opening fraction.

## Control logic

Supported control policies include:

- `AlwaysClosed`;
- `AlwaysOpen`;
- `Schedule`;
- `TemperatureDriven`;
- `CoolingAssist`;
- `NightPurge`;
- `Custom`.

Deterministic lockouts are supported:

- maximum wind speed;
- heating lockout;
- cooling lockout.

## Formula conventions

Deterministic airflow and sensible-lane conventions:

- `Qv = Cd * A_eff * sqrt(2 * |dP| / rho)`;
- `Hve = m_dot * cp`;
- sensible load sign convention in this lane:
  - `Q_ve = Hve * (T_indoor - T_outdoor)`.
  - positive value: indoor-to-outdoor heat-loss tendency.
  - negative value: outdoor-to-indoor heat-gain tendency.

For prescribed hourly airflow mode:

- `Hve = rho * cp * Qv`;
- same sensible sign convention is applied.

## Topology and validation rules

Opening topology validation includes:

- missing zone or room references;
- missing boundary/surface reference (strict mode);
- boundary reference to non-exterior surfaces rejected (`Ground`, `Adiabatic`, internal/adjacent boundaries);
- non-positive opening area rejected;
- invalid opening fractions rejected;
- invalid prescribed airflow or wind speed rejected;
- deterministic, sorted diagnostics.

## ISO52016 / MultiZone handoff

Natural ventilation hourly `Hve` can be mapped to the MultiZone ventilation/infiltration conductance lane through normalized-input build integration.

Supported merge policies:

- no-double-counting max policy (default);
- additive policy (explicit opt-in);
- natural-only replacement policy.

Component-aware diagnostics are emitted for infiltration/natural/mechanical/custom lane composition.

## Known limitations

- no full airflow network solver;
- no detailed cross-ventilation network solver;
- no inter-zone airflow solver;
- no moisture/latent effects;
- no pollutant/IAQ model.

## Explicit non-claims

- This is not a full ISO52016 compliance claim.
- This does not claim one-to-one equivalence with external third-party tools.
- This does not claim external-engine numerical identity.
- This is an internal engineering calculation implementation with deterministic tests and validation anchors.
