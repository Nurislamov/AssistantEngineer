# AssistantEngineer Frontend

React + TypeScript frontend для ASP.NET Core Web API проекта `AssistantEngineer`.

## Как запустить

```bash
cd src/Frontend
npm install
npm run dev
```

После запуска Vite откроет приложение на `http://localhost:5173`.

## Настройка API base URL

Создайте `.env` рядом с `package.json`:

```env
VITE_API_BASE_URL=http://localhost:5000
VITE_API_VERSION=1
VITE_DEFAULT_PROJECT_ID=1
```

Текущий backend использует версионированные endpoint'ы вида `/api/v1/...`, поэтому версия API вынесена в `VITE_API_VERSION`. Если проекта с `VITE_DEFAULT_PROJECT_ID` нет, frontend берёт первый проект из `GET /api/v1/projects`; если проектов нет, показывает создание проекта через `POST /api/v1/projects`.

## Архитектура папок

Структура близка к Feature-Sliced Design, но без лишней сложности:

- `src/app` - провайдеры, роутер, layout и тема приложения.
- `src/shared` - общий API-клиент, env/config, UI-компоненты, форматтеры, константы.
- `src/entities` - DTO, API-адаптеры и query hooks для доменных сущностей.
- `src/features` - пользовательские действия: создать здание, добавить помещение, запустить расчёт, скачать отчёт.
- `src/widgets` - крупные составные блоки страниц: sidebar, header, summary.
- `src/pages` - тонкие страницы, которые собирают widgets/features/entities.

Такую структуру удобно расширять: UI не знает деталей `fetch`, страницы не содержат бизнес-логику, а реальные backend-контракты изолированы в API-адаптерах.

## Как добавлять новую feature

1. Создайте папку в `src/features/<domain>/<feature-name>`.
2. Положите React Query mutation/query orchestration в `model`.
3. Положите форму или кнопку в `ui`.
4. Импортируйте feature в страницу или widget.

Страница должна только собирать готовые блоки, а не выполнять API-запросы напрямую.

## Как подключить новый endpoint

1. Добавьте маршрут в `src/shared/api/apiRoutes.ts`.
2. Добавьте DTO и backend response type в `src/entities/<entity>/types.ts`.
3. Добавьте метод API в `src/entities/<entity>/api`.
4. Если backend response отличается от frontend DTO, сделайте mapper в API-слое или `lib`.
5. Подключите метод через React Query hook в `model`.

## Как поменять UI-kit или тему

Тема MUI находится в `src/app/providers/theme.ts`.

Общие визуальные элементы вынесены в `src/shared/ui`: `PageHeader`, `PageContainer`, `LoadingState`, `ErrorState`, `QueryState`, `EmptyState`, `DataCard`, `FormDialog`, `ConfirmDeleteButton`. При замене MUI сначала заменяйте эти компоненты, а затем постепенно feature/widgets.

## Реальные backend endpoint'ы

Frontend адаптирован под текущие контроллеры backend:

- здания читаются через `/api/v1/projects/{projectId}/buildings`;
- создание здания идёт через `/api/v1/projects/{projectId}/buildings`;
- помещения создаются через `/api/v1/rooms`, потому что backend требует `floorId`;
- помещения здания собираются через этажи: `/api/v1/buildings/{buildingId}/floors` + `/api/v1/rooms?floorId=...`;
- расчёты запускаются через GET endpoint'ы `/load-calculations/cooling-load` и `/load-calculations/heating-load`;
- Excel-отчёт скачивается через `/api/v1/reports/buildings/{buildingId}/energy-balance/excel`.

## Временные TODO

- `VITE_DEFAULT_PROJECT_ID` задаёт предпочитаемый проект. Если его нет, используется первый доступный проект. Позже можно добавить полноценный выбор проекта в UI.
- В backend сейчас не видно `PUT/DELETE` endpoint'ов для зданий и помещений. Методы и placeholder-компоненты оставлены в структуре под будущую интеграцию.
- Нет backend endpoint'а `latest calculation`, поэтому последний результат хранится в React Query cache после запуска расчёта. После обновления страницы результат нужно запустить заново или подключить реальный endpoint.
- Подбор оборудования пока представлен отдельной страницей-заготовкой.
- Диаграммы и PDF/Excel отчёты можно подключать отдельными features без изменения текущей структуры.
