# AssistantEngineer Frontend

React, Vite, TypeScript, React Query, and MUI frontend for the AssistantEngineer API.

## Run Locally

```powershell
cd src/Frontend
npm ci
copy .env.example .env
npm run dev
```

The API base URL defaults to `http://localhost:5194`. Override it in `.env` when the backend runs elsewhere:

```env
VITE_API_BASE_URL=http://localhost:5194
VITE_API_VERSION=1
```

Project selection is handled in the UI. The app lists projects from `GET /api/v1/projects`, stores the selected project in local storage, and shows project creation when no project exists.

## Structure

- `src/app` - providers, router, layout, and theme.
- `src/shared` - API client, route builders, config, common UI, and formatting helpers.
- `src/entities` - typed DTOs, mappers, API adapters, and entity query hooks.
- `src/features` - user actions and feature-level state.
- `src/widgets` - composed page sections such as header, sidebar, and building workspace.
- `src/pages` - thin route components that compose widgets and features.

Pages should not call `fetch` directly. Add backend integration through `shared/api` route builders and entity API adapters.

## Build

```powershell
cd src/Frontend
npm ci
npm run build
```
