# Engineering Core V1 API Contract Changelog

## v1

Initial Engineering Core V1 API contract package.

Added endpoints:

- GET /api/v1/calculations/engineering-core/v1/status
- GET /api/v1/calculations/engineering-core/v1/diagnostics-catalog

Added contract artifacts:

- openapi.fragment.yml
- postman_collection.json
- status.sample.json
- diagnostics-catalog.sample.json
- engineering-core-v1.http
- ConsumerGuide.md

Contract status:

- ClosedV1 as engineering formula gate;
- no exact EnergyPlus numerical equivalence claim;
- no exact StandardReference numerical equivalence claim;
- no ASHRAE 140 / BESTEST-style validation anchor coverage claim;
- no full ISO 52016 node/matrix solver equivalence claim;
- no latent/moisture/humidity support in v1.

Compatibility:

- additive response fields are allowed;
- removing required fields is a breaking change;
- changing diagnostics severity semantics is a breaking change;
- weakening annual 8760 requirements is a breaking change;
- removing explicit non-claims is a breaking change.
