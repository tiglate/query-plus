# AGENTS.md

Guidance for coding agents working in this repository. `CLAUDE.md` mirrors this file — keep both in sync when editing.

## What this is

Query Plus — governed web app that lets business users discover and execute predefined SQL Server stored procedures, with a catalog (categories/procedures/parameters/columns), RBAC via Keycloak, audit trail, server-side pagination, and background Excel export. Backend: .NET 10 / ASP.NET Core Web API (controllers returning JSON — no Razor, no MVC views, no HTMX). Frontend: React 19 + TypeScript SPA (Vite, Tailwind 4, Radix/shadcn, TanStack Query, React Router 7, react-i18next) served as static assets by the API in production.

Full product requirements: `docs/SPECIFICATION.md`. Setup/config: `README.md`. `docs/local/` holds gitignored private planning notes — not shipped, not for GitHub.

## Commands

### Backend (.NET)

`global.json` pins SDK 10.0.x (rollForward `latestFeature`, prereleases allowed).

```bash
dotnet restore
dotnet build QueryPlus.sln          # builds SPA only if src/QueryPlus.Api/wwwroot/index.html is missing
dotnet test QueryPlus.sln --filter "Category!=Integration"   # fast xUnit suite (Application + Data + Api) — no Docker needed
dotnet test tests/QueryPlus.Integration.Tests                # real-SQL-Server tests via Testcontainers — needs Docker
dotnet test tests/QueryPlus.Application.Tests   # single project
dotnet test --filter "FullyQualifiedName~ProcedureServiceTests"  # single class/test

dotnet run --project src/QueryPlus.Api          # http://localhost:5132 (http profile)
```

`dotnet test QueryPlus.sln` with no filter also picks up `tests/QueryPlus.Integration.Tests` and will fail/hang without Docker — always pass `--filter "Category!=Integration"` for the routine fast-feedback loop.

EF Core migrations (also applied automatically on startup by `DemoDataSeeder`):

```bash
dotnet ef database update --project src/QueryPlus.Data --startup-project src/QueryPlus.Api
dotnet ef migrations add <Name> --project src/QueryPlus.Data --startup-project src/QueryPlus.Api
```

### Frontend (React SPA, from `client/queryplus-react`)

```bash
pnpm install && pnpm run build   # or: vp install / vp build
pnpm run dev                     # or: vp dev — Vite on http://localhost:5173, proxies /api + /login to the API
pnpm test                        # or: vp test — Vitest + jsdom
pnpm run check                   # or: vp check (tsc + eslint + prettier)
pnpm run gen:api                 # placeholder; contracts live in src/api/types.ts
```

Run a single Vitest file: `pnpm exec vitest run src/features/home/components/results-grid.test.tsx`.

Rebuild rule of thumb: any edit under `client/queryplus-react/` requires `pnpm run build` (or `dev` watch) — `dotnet run` alone will **not** pick it up if `src/QueryPlus.Api/wwwroot/index.html` already exists. Pure C# changes need no frontend rebuild. The Vite output (`src/QueryPlus.Api/wwwroot/`) is gitignored/build output. Publish always rebuilds the SPA; skip with `/p:SkipClientAppBuild=true`.

Vite is configured with a **single non-split entry** at `assets/queryplus.js` (code splitting intentionally disabled — a split vendor chunk previously caused a double-mount bug on the ResultsMaximize overlay). Don't re-enable it without understanding that history.

### Infra (local dev)

```bash
cp .env.example .env                         # once, before anything else
docker compose up -d sqlserver keycloak       # SQL Server :1433, Keycloak :8080 (realm "queryplus")
docker compose --profile full up --build      # full stack incl. API + SPA, http://localhost:5000
```

`.env` is gitignored and loaded by both Docker Compose and `EnvFileLoader` (`src/QueryPlus.Api/Hosting/EnvFileLoader.cs`) into `dotnet run`, without overriding vars already set by the shell/CI. Never put real secrets in `appsettings*.json` — those hold non-secret defaults only.

## Architecture

Clean/layered, one project per layer, dependencies point inward:

```
QueryPlus.Api              Web API controllers (JSON), OIDC/auth adapters, ProblemDetails, static SPA host, DI composition
QueryPlus.Application      use cases/services, DTOs, FluentValidation validators, interfaces
QueryPlus.Domain           entities (int PKs), repository/UoW interfaces, domain exceptions — no EF/Dapper deps
QueryPlus.Infrastructure   thin composition root wiring Data (+ future integrations) into DI
QueryPlus.Data             EF Core DbContext/repositories (CRUD + migrations), Dapper stored-proc executor, demo seed
```

Each layer exposes an `AddXxx(IServiceCollection)` extension under its own `DependencyInjection/` folder; `Program.cs` is a thin composition root that wires the API host (services → authentication → controllers → SPA fallback → endpoints), then `SeedDemoDataAsync()`. Follow this pattern for new cross-cutting registrations rather than adding directly to `Program.cs`.

- **EF Core** owns catalog CRUD (categories, procedures, parameters, columns) and migrations.
- **Dapper/ADO.NET** (`DapperStoredProcedureExecutor`) executes catalogued stored procedures dynamically and returns a `DataTable` — this is the only path that runs arbitrary-shaped SQL, and it is parameterized, never string-concatenated.
- All domain entities use `int` identity PKs (not GUIDs).
- Configuration changes to Category/Procedure/Parameter/Column are tracked in `*_aud` audit tables via `AuditSaveChangesInterceptor` (revision pattern, see `RevisionType`/`Revision`).

### Security-sensitive code (read before touching)

- `QueryPlus.Application/Common/SqlIdentifier.cs` — validates/quotes DB, schema, and procedure names before building three-part names for dynamic execution. Any new code path that composes a SQL identifier from user/catalog data must go through this.
- `QueryPlus.Application/Common/ParameterSecurity.cs` — defense-in-depth sanitization of FreeText parameter values (control-char stripping, LIKE-metacharacter escaping, suspicious-fragment rejection) even though execution is already parameterized via ADO.NET.
- `QueryPlus.Application/Common/ProcedurePagination.cs` — reserved pagination parameter names (`@PageNumber`, `@PageSize`, `@TotalRecords`) that catalog parameters may never use; enforced both at admin validation time and at metadata-sync time.
- API pipeline denies by default: controllers opt out explicitly with `[AllowAnonymous]` rather than opting in to auth. Unauthenticated callers receive `401 Unauthorized` JSON (not a redirect); React intercepts and routes to `/login`. Antiforgery: state-changing endpoints require an `X-CSRF-TOKEN` header that matches the antiforgery cookie; the SPA fetches the token from `GET /api/csrf` and echoes it on subsequent mutations.

### Server-side pagination contract

Procedures flagged `supports_pagination` on `tb_procedure` implement a fixed, non-catalog contract: `@PageNumber BIGINT = 1`, `@PageSize BIGINT = 50`, `@TotalRecords BIGINT OUTPUT`. The Execute API injects these and reads the OUTPUT total; interactive UI page size is capped (`ProcedurePagination.MaxUiPageSize`), while Excel export re-executes with `@PageNumber = 1` and a giant `@PageSize` (`ExportPageSize`) to pull the full result set. ADO.NET command timeout for stored-proc execution is 30 minutes (`ProcedurePagination.CommandTimeoutSeconds`).

### Excel export flow

Export is queued to a background worker (`ExcelExportBackgroundService`, `src/QueryPlus.Api/Services/ExcelExportService.cs`) after a successful execute with data; eligibility is tied to the last successful execute (procedure + parameter values, TTL-bound — `ExportEligibilityService`). React polls status via TanStack Query (`refetchInterval` while pending); the API serves downloads at `GET /api/exports/{jobId}/download`. Output files land in `App_Data/exports` (gitignored, runtime-only).

### Auth (Keycloak / OIDC)

Authorization-code flow, cookie session (`QueryPlus.Auth`) after login. Two Keycloak-facing URLs matter and are easy to confuse:
- `Keycloak:Authority` — public URL the **browser** must be redirected to (e.g. `http://localhost:8080/realms/queryplus`).
- `Keycloak:MetadataAddress` / `Keycloak:BackchannelHost` — internal Docker DNS name (`keycloak`) used only for server-to-server discovery/token/JWKS calls.

`KeycloakUrlRewriter` and `KeycloakBackchannelHttpHandler` (`src/QueryPlus.Api/Auth/`) exist specifically so the browser is never redirected to the Docker-internal `keycloak` hostname while the server still talks to Keycloak over the Docker network. The SPA lives at the **same origin** as the API in production (`/` is `index.html`, `/api/*` is the API); in development Vite serves on `:5173` and proxies `/api` and `/login` to the API on `:5132`. When touching auth config, preserve this split and the same-origin cookie assumption.

### Web API layer conventions

- Controllers live under `src/QueryPlus.Api/Api/`, organized by feature (Auth, Categories, Procedures, Execute, Exports, ExecutionLogs, Health). JSON in/out only — no Razor, no ViewResults.
- Antiforgery is global for state-changing endpoints; the SPA pattern is `GET /api/csrf` → echo via `X-CSRF-TOKEN` header on `POST`/`PUT`/`DELETE`.
- ProblemDetails (`AddProblemDetails()` + `ApiExceptionHandler`) handles error responses; never return raw `Exception.Message` to the wire.
- `JsonStringTrimConverter` (System.Text.Json) trims bound string properties where the API contract requires it (no global `TrimStringModelBinder`).
- Localization: `pt-BR` (default) and `en`, switchable via `?culture=`, cookie, or `Accept-Language`. The SPA owns its own `react-i18next` bundles under `client/queryplus-react/src/i18n/{en,pt-BR}.json`.

### React SPA (client/queryplus-react) conventions

- React Router 7 `createBrowserRouter` + TanStack Query v5. Auth-required routes use a `loader` that calls `queryClient.ensureQueryData(['auth','user'])`.
- `apiFetch(path, init)` wrapper sets `credentials: 'include'`, parses JSON, throws `ApiError` on non-2xx. CSRF: on first mutation, `GET /api/csrf` then attach `X-CSRF-TOKEN` header for the rest of the session.
- Feature folders mirror pages: `features/{home,admin/categories,admin/procedures,admin/execution-logs}` plus `components/` (shadcn/ui generated under `components/ui/`, app wrappers at top level) and `api/hooks/`.
- `sheet-grid/` virtualized results grid uses `@tanstack/react-virtual`. The Vite `entryFileNames: "assets/queryplus.js"` and `inlineDynamicImports: true` produce a single non-split bundle to avoid the ResultsMaximize double-mount regression.
- Tests: Vitest + Testing Library under `client/queryplus-react/src/**/*.test.tsx`, one spec per unit roughly mirroring the source layout.

## Testing conventions

- `tests/QueryPlus.Application.Tests` — service/validator/helper unit tests (xUnit, FluentAssertions, NSubstitute for mocked repos/services).
- `tests/QueryPlus.Api.Tests` — controller unit tests plus HTTP-level integration tests via `QueryPlusApiApplicationFactory` (a `WebApplicationFactory<Program>` that swaps in NSubstitute fakes for `IProcedureService`/`ICategoryService`/`IExecutionService`/`IExcelExportService`/`IProcedureRepository`/`IProcedureMetadataSyncService` and a `TestAuthHandler` in place of real OIDC). Use the factory's exposed substitute properties to set up scenario expectations rather than hitting a real database — the factory intentionally points at an unreachable SQL Server connection string. `AntiforgeryApiHelper` handles the CSRF bootstrap dance for state-changing integration tests; `AnonymousQueryPlusApiApplicationFactory` exercises the anonymous/anonymous-only endpoints.
- Security-relevant logic (`SqlIdentifier`, `ParameterSecurity`, `ProcedurePagination`) has dedicated `*SecurityTests.cs`/`*Tests.cs` files; extend these rather than testing the same rules indirectly through a service.
- `tests/QueryPlus.Data.Tests/UnitOfWorkTests.cs` uses the SQLite in-memory relational provider (`Microsoft.Data.Sqlite`, connection kept open for the test's lifetime) rather than EF's InMemory provider, because `UnitOfWork` wraps `SaveChangesAsync` in a real transaction/execution-strategy that the InMemory provider doesn't support. It deliberately stops short of proving `AuditSaveChangesInterceptor`'s audit rows commit/roll back atomically with the principal row — that needs a real relational engine and is covered in `QueryPlus.Integration.Tests` instead.
- `tests/QueryPlus.Integration.Tests` — real-SQL-Server tests via **Testcontainers.MsSql** (needs Docker locally; every class is tagged `[Trait("Category", "Integration")]` and excluded from the routine `dotnet test` filter above). One `MsSqlContainer` is shared for the whole run via a `[Collection("Integration")]` fixture (`SqlServerContainerFixture`); each test class inherits `IntegrationTestBase`, which creates its own throwaway database on that shared server, wires the real production DI graph (`AddApplication()` + `AddData()` — no test doubles), and runs `DemoDataSeeder` before every test (xUnit constructs the class fresh per `[Fact]`, so isolation is per-test, not just per-class). This is the only place `DapperStoredProcedureExecutor` and `SqlProcedureMetadataSyncService` get real coverage — both construct their `SqlConnection` internally (not injected) and query real SQL Server internals (`sys.parameters`, `sys.sp_describe_first_result_set`), so they can't be unit-tested with mocks. Covers: migrations + demo-seed apply cleanly (asserted positively, since `DemoDataSeeder` swallows install failures into a logged warning), repository CRUD against a real engine, `AuditSaveChangesInterceptor` audit-row commit/rollback atomicity, and `DapperStoredProcedureExecutor` pagination/OUTPUT-parameter round-tripping against `dbo.Sp_Demo_Numbers_Paged` (installed by the seeder).