# QueryPlus

Governed execution of SQL Server stored procedures for business users — with catalog management, RBAC, audit trail, and Excel export.

Built with **.NET 10**, **ASP.NET Core Web API (controllers)**, **React 19 + TypeScript (Vite SPA)**, **EF Core**, **Dapper**, **Tailwind 4**, and **Keycloak** (OpenID Connect).

**Default language:** Brazilian Portuguese (`pt-BR`), with English (`en`).

## ✨ Features

- 🏠 **Home** — pick a catalogued procedure, set parameters, execute, page large results server-side, export to Excel
- 🗂️ **Admin** — manage categories and procedures (parameters, columns, sync metadata from SQL Server)
- 🔐 **Security** — OIDC via Keycloak (cookie session + antiforgery); procedure-level role entitlements; reserved pagination args never exposed to end users
- 📋 **Ops** — execution log, configuration audit tables, demo data seeded on startup

## 📦 Solution structure

```
QueryPlus.sln
src/
  QueryPlus.Api               # ASP.NET Core Web API (controllers) + OIDC + static SPA host
  QueryPlus.Application       # Application services, DTOs, FluentValidation validators
  QueryPlus.Domain            # Entities, repository contracts (INT PKs)
  QueryPlus.Infrastructure    # Composition root for external concerns
  QueryPlus.Data              # EF Core CRUD + Dapper stored procedure execution
tests/
  QueryPlus.Application.Tests
  QueryPlus.Data.Tests
  QueryPlus.Api.Tests         # controller unit + HTTP integration via WebApplicationFactory
client/
  queryplus-react/            # React 19 + Vite + TS SPA (Tailwind 4, TanStack Query, Radix/shadcn)
docker/
  keycloak/realm-export.json  # Dev realm (users: demo/demo, admin/admin)
.devcontainer/                # VS Code / Codespaces Dev Containers
docs/
  SPECIFICATION.md
  database/                   # schema + demo SQL mirrors
```

### Layering

| Layer | Responsibility |
|-------|----------------|
| **Domain** | Entities (`int` PKs), repository/UoW interfaces |
| **Application** | Use cases, service interfaces, DTOs, FluentValidation validators (`SqlIdentifier`, `ParameterSecurity`, `ProcedurePagination`) |
| **Data** | EF Core `DbContext`/repositories (CRUD + migrations), Dapper executor (`DataTable`), `DemoDataSeeder`, `AuditSaveChangesInterceptor` |
| **Infrastructure** | Wires Data + future integrations into DI |
| **Api** | Web API controllers, OIDC auth, ProblemDetails, SPA static host, DI composition |

- **EF Core** — catalog CRUD and migrations
- **Dapper / ADO.NET** — dynamic stored procedure results as `DataTable`
- **React SPA** — `client/queryplus-react/`, Vite dev on `:5173` (proxies `/api` and `/login` to the API), production build emits to `src/QueryPlus.Api/wwwroot/` and is served by the API as static files

## ✅ Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (SQL Server + Keycloak)
- [Node.js 22+](https://nodejs.org/) + [pnpm](https://pnpm.io/) 10+ for the React SPA — first build and any frontend work

## 🚀 Quick start (local)

### 0. Configure local secrets (required)

Credentials are **not** stored in `appsettings*.json`. Copy the template and edit only if needed:

```bash
cp .env.example .env
```

| File | In git? | Purpose |
|------|---------|---------|
| `.env.example` | ✅ yes | Dummy local defaults (template) |
| `.env` | ❌ **never** (gitignored) | Your machine-only values |

⚠️ **These values are dummy development credentials.** They exist only so Docker and a laptop can start quickly. **Do not use them in production, staging, or any shared environment.** Rotate secrets, use a secret manager, and enforce HTTPS/strong passwords before any real deployment.

Docker Compose reads `.env` for variable substitution. `dotnet run` also loads a repo-root `.env` into the process environment (without overriding variables already set by the shell or CI).

### 1. Start infrastructure

```bash
docker compose up -d sqlserver keycloak
```

| Service | URL / connection (from `.env.example` dummies) |
|---------|------------------|
| SQL Server | `localhost:1433` (sa / value of `MSSQL_SA_PASSWORD`) |
| Keycloak | http://localhost:8080 (`KEYCLOAK_ADMIN` / `KEYCLOAK_ADMIN_PASSWORD`) |
| Realm | `queryplus` (imported automatically) |
| Demo users | `demo` / `demo` (`ROLE_QUERY_EXEC`, `ROLE_CATEGORY_READ`, `ROLE_PROCEDURE_READ`), `admin` / `admin` (`ROLE_ADMIN`) — realm export, local only |
| OIDC client secret | Must match `Keycloak__ClientSecret` and `docker/keycloak/realm-export.json` |

Realm roles (`docker/keycloak/realm-export.json`) gate API access on top of per-procedure `RoleEntitlement`:

| Role | Grants |
|------|--------|
| `ROLE_ADMIN` | Everything below, unconditionally |
| `ROLE_CATEGORY_READ` / `ROLE_CATEGORY_WRITE` | View/search categories, or full category CRUD |
| `ROLE_PROCEDURE_READ` / `ROLE_PROCEDURE_WRITE` | View/search the procedure catalog, or full procedure CRUD |
| `ROLE_QUERY_EXEC` | Execute procedures and download exports — still subject to each procedure's own `RoleEntitlement` |

### 2. Apply database schema

Migrations also run automatically via `DemoDataSeeder` on app startup. To apply explicitly:

```bash
dotnet tool install --global dotnet-ef   # once
dotnet ef database update \
  --project src/QueryPlus.Data \
  --startup-project src/QueryPlus.Api
```

### 3. Build the React SPA (first time / after frontend changes)

The React SPA lives under `client/queryplus-react/` and builds into `src/QueryPlus.Api/wwwroot/` (gitignored) so `dotnet publish` and `dotnet run` can serve it as static content.

```bash
cd client/queryplus-react
pnpm install          # or: vp install
pnpm run build        # → src/QueryPlus.Api/wwwroot/{assets,index.html}
```

#### When do I need `pnpm run build`?

| Situation | Rebuild SPA? |
|-----------|---------------|
| First clone / empty `src/QueryPlus.Api/wwwroot/index.html` | **Yes** — or `dotnet build` / `dotnet run` (auto-builds when `wwwroot/index.html` is missing) |
| You changed files under `client/queryplus-react/` | **Yes** — `dotnet run` alone will **not** rebuild an existing bundle |
| Only .NET / C# changes | No — `dotnet run` is enough |
| `dotnet publish` or Docker image build | Automatic |

💡 **Tip:** after React/CSS edits, run `pnpm run build` again (or use watch mode below).

#### Day-to-day frontend development

```bash
# Terminal 1 — Vite dev server on http://localhost:5173 (proxies /api + /login to the API)
cd client/queryplus-react
pnpm run dev          # or: vp dev

# Terminal 2 — ASP.NET Core API on http://localhost:5132
dotnet run --project src/QueryPlus.Api
```

```bash
cd client/queryplus-react

pnpm install && pnpm run build && pnpm test && pnpm run dev
```

Skip the MSBuild SPA step when needed:

```bash
dotnet publish ... /p:SkipClientAppBuild=true
```

Vite is configured with `server.port = 5173`, `strictPort`, and a dev proxy `/api` → `http://localhost:5132` (override with `VITE_API_PROXY`). The production bundle is emitted as a single non-split entry at `assets/queryplus.js`.

### 4. Run the API

```bash
dotnet run --project src/QueryPlus.Api
```

Open the URL printed by Kestrel — `http://localhost:5132` for the `http` launch profile (or `https://localhost:7192` for the `https` profile). The same origin serves the JSON API (`/api/...`), the Keycloak login/logout endpoints (`/login`, `/logout`, `/signout-callback`), and the built SPA (`/`).

Configure Keycloak client redirect URIs to match that origin if needed (`docker/keycloak/realm-export.json`).

## 🧪 Build & test

```bash
dotnet restore
dotnet build QueryPlus.sln    # builds SPA only if src/QueryPlus.Api/wwwroot/index.html is missing
dotnet test QueryPlus.sln

# SPA unit tests (Vitest + jsdom)
cd client/queryplus-react && pnpm test
```

## 🐳 Docker (full stack)

```bash
docker compose --profile full up --build
```

- API + SPA: http://localhost:5000
- Uses `appsettings.Docker.json` / environment variables for SQL Server and Keycloak.

## 🧰 Dev Containers

1. Open the repo in VS Code / Cursor.
2. **Dev Containers: Reopen in Container**.
3. SQL Server and Keycloak start via Compose; .NET 10 is available in the `app` service.

```bash
dotnet run --project src/QueryPlus.Api --urls http://0.0.0.0:5000
```

## ⚙️ Configuration

Prefer **environment variables** (including those from `.env`) over committing secrets.

| Setting / env var | Description |
|-------------------|-------------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `Keycloak__Authority` | e.g. `http://localhost:8080/realms/queryplus` |
| `Keycloak__ClientId` | `queryplus-web` |
| `Keycloak__ClientSecret` | OIDC client secret (**dummy in `.env.example` only**) |
| `Keycloak__RequireHttpsMetadata` | `false` for local HTTP Keycloak |
| `MSSQL_SA_PASSWORD` | SQL Server `sa` password for Compose |
| `KEYCLOAK_ADMIN` / `KEYCLOAK_ADMIN_PASSWORD` | Keycloak admin user for Compose |

`appsettings.json` holds non-secret defaults (logging, public Authority/ClientId). Secrets should come from `.env`, the host environment, or a production secret store—not from source control.

Localization: `?culture=pt-BR` or `?culture=en` (also cookie / `Accept-Language`).

## 🔑 Authentication notes

- OpenID Connect authorization code flow against Keycloak.
- Cookie session after login (`QueryPlus.Auth`).
- `/login` challenges OIDC; `/logout` and `/signout-callback` complete front- and back-channel sign-out (antiforgery protected for `POST /logout`).
- The SPA talks to the API **same-origin** (production) or via the Vite dev proxy (development). The API requires an antiforgery token + `X-CSRF-TOKEN` header on state-changing requests (Bootstrap pattern: `GET /api/csrf` then echo via header). All API endpoints except `/api/auth/*`, `/api/health`, `/api/csrf` and `/login` require authentication; missing/invalid auth returns `401 Unauthorized` JSON (React intercepts and redirects to `/login`).

### Dev Containers / Docker networking

The browser must never be redirected to the Docker DNS name `keycloak`.

| Setting | Purpose |
|---------|---------|
| `Keycloak:Authority` | Public URL for the browser (`http://localhost:8080/realms/queryplus`) |
| `Keycloak:MetadataAddress` | Internal discovery URL (`http://keycloak:8080/realms/.../.well-known/...`) |
| `Keycloak:BackchannelHost` | Rewrites server token/JWKS calls from `localhost` → `keycloak` |

Keycloak is started with `KC_HOSTNAME=localhost` so issuer/authorize URLs are host-reachable.

```bash
docker compose down
docker compose up -d sqlserver keycloak
# or: Dev Containers → Rebuild Container
```

Before any non-dev environment: use unique strong passwords, a private Keycloak client secret, disable or replace demo users, and never ship a real `.env` or commit production connection strings.

## 🔢 Primary keys

All domain entities use **`int`** identity primary keys.

## 🌱 Demo data (automatic on startup)

On application start, `DemoDataSeeder`:

1. Applies EF Core migrations
2. Installs demo tables + stored procedures from `src/QueryPlus.Data/Seed/demo-objects.sql`
3. Registers categories/procedures/parameters/columns from `demo-catalog.json` (idempotent)

### Highlights

| Object | Purpose |
|--------|---------|
| `tb_usa_president` + list / paged SPs | Presidents list with filters |
| Pagination demos | `Sp_Demo_Numbers_Paged`, `Sp_Demo_Large_Result_Paged`, etc. |
| 30+ `Sp_Demo_*` procedures | FreeText, Numeric, Date, Time, DateTime, Boolean, Combo |
| Supporting tables | customers, products, orders, employees, … |

Role entitlement for demo procedures is **`ROLE_QUERY_EXEC`** (also works for `ROLE_ADMIN`, which implies every permission).

SQL scripts are also mirrored under `docs/database/`.

## 📚 Documentation

- [Software specification](docs/SPECIFICATION.md)
- [Database schema](docs/database/schema.sql)

## 📄 License

This project is licensed under the [MIT License](LICENSE).