# ADR Index

## Purpose

This index tracks accepted architecture decision records and their stage linkage.

## Records

- `ADR-0001`
  - title: Security governance boundary and ownership backfill write-path disabled state
  - status: Accepted
  - date: 2026-05-20
  - path: `docs/adr/ADR-0001-security-governance-boundary.md`
  - related stages: `P5`, `P6`, `P7-00`, `P7-01`, `P7-06`, `P7-07`

## Decision matrices

- `security-architecture-decision-matrix`
  - stage: `P7-08`
  - path: `docs/adr/security-architecture-decision-matrix.md`
  - json: `docs/adr/security-architecture-decision-matrix.json`
  - purpose: Consolidates accepted/deferred/rejected security architecture decisions across P5/P6/P7.

## Companion governance backlogs

- `future-security-adr-backlog`
  - stage: `P7-08`
  - path: `docs/adr/future-security-adr-backlog.md`
  - json: `docs/adr/future-security-adr-backlog.json`
  - purpose: Tracks deferred security decisions that must be handled by dedicated future ADRs.

## Notes

- ADR records define governance/architecture decisions and do not directly enable runtime write paths.
