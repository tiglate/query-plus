# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Query Plus — governed web app that lets business users discover and execute predefined SQL Server stored procedures, with a catalog (categories/procedures/parameters/columns), RBAC via Keycloak, audit trail, server-side pagination, and background Excel export. Backend: .NET 10 / ASP.NET Core MVC (Controllers + Views, no Razor Pages, no Web API). Frontend: server-rendered Razor + HTMX + a TypeScript "ClientApp" (Tailwind 4, Vite) — not a SPA.

Full product requirements: `docs/SPECIFICATION.md`. Setup/config: `README.md`. `docs/local/` holds gitignored private planning notes — not shipped, not for GitHub.

## Commands

### Backend (.NET)

```bash
dotnet restore
dotnet build QueryPlus.sln          # builds ClientApp only if wwwroot/dist/js/app.js is missing
dotnet test QueryPlus.sln           # all xUnit tests (Application + Web)
dotnet test tests/QueryPlus.Application.Tests   # single project
dotnet test --filter "FullyQualifiedName~ProcedureServiceTests"  # single class/test

dotnet run --project src/QueryPlus.Web
```

EF Core migrations (also applied automatically on startup by `DemoDataSeeder`):

```bash
dotnet ef database update --project src/QueryPlus.Data --startup-project src/QueryPlus.Web
dotnet ef migrations add <Name> --project src/QueryPlus.Data --startup-project src/QueryPlus.Web
```

### Frontend (ClientApp, from `src/QueryPlus.Web`)

```bash
pnpm install && pnpm run build   # or: vp install / vp build
pnpm run dev                     # or: vp dev — watch mode, run alongside dotnet run
pnpm test                        # or: vp test — Vitest + jsdom
pnpm run check                   # or: vp check
```

Run a single Vitest file: `pnpm exec vitest run ClientApp/tests/sheet-grid-service.test.ts`.

Rebuild rule of thumb: any edit under `ClientApp/` requires `pnpm run build` (or `dev` watch) — `dotnet run` alone will **not** pick it up if `wwwroot/dist` already exists. Pure C#/Razor changes need no frontend rebuild. `wwwroot/dist` is gitignored/build output.

### Infra (local dev)

```bash
cp .env.example .env                         # once, before anything else
docker compose up -d sqlserver keycloak       # SQL Server :1433, Keycloak :8080 (realm "queryplus")
docker compose --profile full up --build      # full stack incl. app, http://localhost:5000
```

`.env` is gitignored and loaded by both Docker Compose and `EnvFileLoader` (`src/QueryPlus.Web/Hosting/EnvFileLoader.cs`) into `dotnet run`, without overriding vars already set by the shell/CI. Never put real secrets in `appsettings*.json` — those hold non-secret defaults only.

## Architecture

Clean/layered, one project per layer, dependencies point inward:

```
QueryPlus.Web            MVC Controllers + Views, ClientApp, OIDC/auth adapters, DI composition
QueryPlus.Application     use cases/services, DTOs, FluentValidation validators, interfaces
QueryPlus.Domain          entities (int PKs), repository/UoW interfaces, domain exceptions — no EF/Dapper deps
QueryPlus.Infrastructure  thin composition root wiring Data (+ future integrations) into DI
QueryPlus.Data            EF Core DbContext/repositories (CRUD + migrations), Dapper stored-proc executor, demo seed
```

Each layer exposes an `AddXxx(IServiceCollection)` extension under its own `DependencyInjection/` folder; `Program.cs` is a thin composition root that just calls `AddApplication()` → `AddInfrastructure()` → `AddWebServices()` → `AddWebMvc()` → `AddWebLocalization()` → `AddWebAuthentication()`, then `SeedDemoDataAsync()`, `UseWebPipeline()`, `MapWebEndpoints()`. Follow this pattern for new cross-cutting registrations rather than adding directly to `Program.cs`.

- **EF Core** owns catalog CRUD (categories, procedures, parameters, columns) and migrations.
- **Dapper/ADO.NET** (`DapperStoredProcedureExecutor`) executes catalogued stored procedures dynamically and returns a `DataTable` — this is the only path that runs arbitrary-shaped SQL, and it is parameterized, never string-concatenated.
- All domain entities use `int` identity PKs (not GUIDs).
- Configuration changes to Category/Procedure/Parameter/Column are tracked in `*_aud` audit tables via `AuditSaveChangesInterceptor` (revision pattern, see `RevisionType`/`Revision`).

### Security-sensitive code (read before touching)

- `QueryPlus.Application/Common/SqlIdentifier.cs` — validates/quotes DB, schema, and procedure names before building three-part names for dynamic execution. Any new code path that composes a SQL identifier from user/catalog data must go through this.
- `QueryPlus.Application/Common/ParameterSecurity.cs` — defense-in-depth sanitization of FreeText parameter values (control-char stripping, LIKE-metacharacter escaping, suspicious-fragment rejection) even though execution is already parameterized via ADO.NET.
- `QueryPlus.Application/Common/ProcedurePagination.cs` — reserved pagination parameter names (`@PageNumber`, `@PageSize`, `@TotalRecords`) that catalog parameters may never use; enforced both at admin validation time and at metadata-sync time.
- MVC pipeline denies by default: `AddWebMvc` registers a global `AuthorizeFilter` requiring an authenticated user; new endpoints must opt out explicitly with `[AllowAnonymous]` rather than opting in to auth.

### Server-side pagination contract

Procedures flagged `supports_pagination` on `tb_procedure` implement a fixed, non-catalog contract: `@PageNumber BIGINT = 1`, `@PageSize BIGINT = 50`, `@TotalRecords BIGINT OUTPUT`. Home injects these and reads the OUTPUT total; interactive UI page size is capped (`ProcedurePagination.MaxUiPageSize`), while Excel export re-executes with `@PageNumber = 1` and a giant `@PageSize` (`ExportPageSize`) to pull the full result set. ADO.NET command timeout for stored-proc execution is 30 minutes (`ProcedurePagination.CommandTimeoutSeconds`).

### Excel export flow

Export is queued to a background worker (`ExcelExportBackgroundService`, `src/QueryPlus.Web/Services`) after a successful Home execute with data; eligibility is tied to the last successful execute (procedure + parameter values, TTL-bound — `ExportEligibilityService`). Client polls status; `ExportsController` serves `/exports/download/{jobId}`. Output files land in `App_Data/exports` (gitignored, runtime-only).

### Auth (Keycloak / OIDC)

Authorization-code flow, cookie session (`QueryPlus.Auth`) after login. Two Keycloak-facing URLs matter and are easy to confuse:
- `Keycloak:Authority` — public URL the **browser** must be redirected to (e.g. `http://localhost:8080/realms/queryplus`).
- `Keycloak:MetadataAddress` / `Keycloak:BackchannelHost` — internal Docker DNS name (`keycloak`) used only for server-to-server discovery/token/JWKS calls.

`KeycloakUrlRewriter` and `KeycloakBackchannelHttpHandler` (`src/QueryPlus.Web/Auth/`) exist specifically so the browser is never redirected to the Docker-internal `keycloak` hostname while the server still talks to Keycloak over the Docker network. When touching auth config, preserve this split.

### Web layer conventions

- Controllers live under `Controllers/` (+ `Controllers/Admin/` for Categories/Procedures management); views under `Views/` mirroring controller names, shared partials under `Views/Shared/Partials/`.
- `TrimStringModelBinder` globally trims all bound form/query/route strings — don't add per-field `.Trim()` calls to compensate for its absence.
- HTMX drives partial-page updates (results grid, pager, export status) instead of a JS SPA; look at `Views/Shared/Partials/_ResultsGrid.cshtml`, `_Pager.cshtml`, `_ExportStatus.cshtml` for the pattern before adding new HTMX endpoints.
- Localization: `pt-BR` (default) and `en`, switchable via `?culture=`, cookie, or `Accept-Language`; resource strings in `Resources/SharedResource.{pt-BR,en}.resx`.

### ClientApp (TypeScript) conventions

Single Vite entry (`ClientApp/src/entries/app.ts`) bundles to `wwwroot/dist/js/app.js` + `css/site.css` — no other bundles, no CDNs load in the layout. Code splitting is deliberately disabled in `vite.config.ts` (a split vendor chunk previously caused a double-mount bug on Maximize) — don't re-enable it without understanding that history.

- DI via `tsyringe`: register new singletons in `ClientApp/src/core/di/container.ts` (`registerAppServices`), inject via constructor. Tests use `createTestContainer(...)` for isolated child containers rather than the global singleton.
- `bootstrap.ts` mounts a `SharedShellController` on every page, then resolves one page controller by page key (`home`, `admin-categories`, `admin-procedures`, `admin-procedure-edit`) — see `resolvePageKey`/`pageKey.ts` for how the key is derived from the DOM. New pages need a new key, a case in `resolvePageController`, and a DI registration.
- Page/shell controllers extend the abstract `PageController` (`mount(root)` / `unmount()` / `dispose()`).
- `components/` holds cross-page widgets (sheet-grid virtualized results grid, nav-dropdown, confirm-submit, parameter-combo, theme); `pages/` holds per-page controllers/services.
- Tests: Vitest + jsdom under `ClientApp/tests/`, one spec per unit roughly mirroring `src/`.

## Testing conventions

- `tests/QueryPlus.Application.Tests` — service/validator/helper unit tests (xUnit, FluentAssertions, NSubstitute for mocked repos/services).
- `tests/QueryPlus.Web.Tests` — controller unit tests plus HTTP-level integration tests via `QueryPlusWebApplicationFactory` (a `WebApplicationFactory<Program>` that swaps in NSubstitute fakes for `IProcedureService`/`ICategoryService`/`IExecutionService`/`IExcelExportService`/`IProcedureRepository`/`IProcedureMetadataSyncService` and a `TestAuthHandler` in place of real OIDC). Use the factory's exposed substitute properties to set up scenario expectations rather than hitting a real database — the factory intentionally points at an unreachable SQL Server connection string.
- Security-relevant logic (`SqlIdentifier`, `ParameterSecurity`, `ProcedurePagination`) has dedicated `*SecurityTests.cs`/`*Tests.cs` files; extend these rather than testing the same rules indirectly through a service.
