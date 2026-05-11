# Frontend Testing Baseline

## Purpose

This document describes the current frontend testing baseline used in AssistantEngineer.

## Current baseline

- **Unit/component baseline:** Vitest + React Testing Library (jsdom).
- **Browser smoke baseline:** Playwright E2E smoke tests.

## E2E smoke scope (P3-06)

- App boots and renders engineering workflow shell.
- Workflow happy-path smoke runs through mocked workflow API responses and verifies result/status rendering.

## Why API mocking is used in smoke

- Browser smoke is kept deterministic and stable.
- Tests do not depend on backend runtime availability.
- No calculation physics is executed in frontend tests.

## What this baseline does not claim

- This is not full frontend E2E coverage.
- This is not real backend end-to-end validation.
- This is not visual regression coverage.
- This is not a full cross-browser compatibility matrix.

## Run commands

From `src/Frontend`:

- `npm run test`
- `npm run test:e2e`

Optional local variants:

- `npm run test:e2e:headed`
- `npm run test:e2e:ui`
