# QueryPlus

QueryPlus is a .NET 10 web application for managing and executing dynamic queries/stored procedures. It follows **clean architecture** with Razor Pages, HTMX, Tailwind CSS, EF Core, Dapper, and Keycloak (OpenID Connect).

**Default language:** Brazilian Portuguese (`pt-BR`), with English (`en`) support.

## Solution structure

```
QueryPlus.sln
src/
  QueryPlus.Web              # Razor Pages + ClientApp (TS/Tailwind) + Keycloak OIDC
  QueryPlus.Application      # Application services & interfaces
  QueryPlus.Domain           # Entities, repository contracts (INT PKs)
  QueryPlus.Infrastructure   # Composition root for external concerns
  QueryPlus.Data             # EF Core CRUD + Dapper stored procedure execution
tests/
  QueryPlus.Application.Tests
  QueryPlus.Web.Tests
docker/
  keycloak/realm-export.json # Dev realm (users: demo/demo, admin/admin)
.devcontainer/               # VS Code / Codespaces Dev Containers
```

### Layering

| Layer | Responsibility |
|-------|----------------|
| **Domain** | Entities (`int` PKs), repository/UoW interfaces |
| **Application** | Use cases, service interfaces (e.g. `IStoredProcedureExecutor`) |
| **Data** | EF Core `DbContext`/repositories (CRUD), Dapper executor (`DataTable`) |
| **Infrastructure** | Wires Data + future integrations into DI |
| **Web** | UI, auth, localization, HTTP endpoints |

- **EF Core** — all standard CRUD.
- **Dapper / ADO.NET** — dynamic stored procedure results as `DataTable`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (SQL Server + Keycloak)
- [Node.js 22+](https://nodejs.org/) + [pnpm](https://pnpm.io/) 10+ for ClientApp (or [Vite+](https://viteplus.dev/) `vp`) — required for first build and any frontend work

## Quick start (local)

### 1. Start infrastructure

```bash
docker compose up -d sqlserver keycloak
```

- SQL Server: `localhost:1433` (sa / `Your_strong_Password123`)
- Keycloak: http://localhost:8080 (admin / admin)
- Realm: `queryplus` (imported automatically)
- Demo users: `demo` / `demo`, `admin` / `admin`

### 2. Apply database schema

```bash
dotnet tool install --global dotnet-ef   # once
dotnet ef database update \
  --project src/QueryPlus.Data \
  --startup-project src/QueryPlus.Web
```

If no migrations exist yet:

```bash
dotnet ef migrations add InitialCreate \
  --project src/QueryPlus.Data \
  --startup-project src/QueryPlus.Web \
  --output-dir Migrations
dotnet ef database update \
  --project src/QueryPlus.Data \
  --startup-project src/QueryPlus.Web
```

### 3. Build the ClientApp (first time / after frontend changes)

Frontend TypeScript, Tailwind 4, HTMX, Clusterize, Font Awesome, and Inter live under  
`src/QueryPlus.Web/ClientApp/` and are built into `wwwroot/dist/` (gitignored).  
Architecture notes: **[docs/frontend-reorganization.md](docs/frontend-reorganization.md)**.

**Prerequisites:** Node.js 22+ and [pnpm](https://pnpm.io/) 10+ (or the [Vite+](https://viteplus.dev/) CLI `vp`).

```bash
cd src/QueryPlus.Web
pnpm install          # or: vp install
pnpm run build        # → wwwroot/dist/{js,css,fonts}
```

#### When do I need `pnpm run build`?

| Situation | Rebuild ClientApp? |
|-----------|--------------------|
| First clone / empty `wwwroot/dist` | **Yes** — or just `dotnet build` / `dotnet run` (auto-builds when `dist/js/app.js` is missing) |
| You changed files under `ClientApp/` | **Yes** — `dotnet run` alone will **not** rebuild an existing `dist` |
| Only .NET / Razor / C# changes | No — `dotnet run` is enough |
| `dotnet publish` or Docker image build | Automatic (`pnpm install --frozen-lockfile` + `pnpm run build`) |

So: **do not assume every `dotnet run` refreshes the frontend.** After TS/CSS/vendor edits, run `pnpm run build` again (or use watch mode below).

#### Day-to-day frontend development (recommended)

Use two terminals so the UI rebuilds as you edit:

```bash
# Terminal 1 — watch ClientApp → wwwroot/dist
cd src/QueryPlus.Web
pnpm run dev          # or: vp dev

# Terminal 2 — ASP.NET
dotnet run --project src/QueryPlus.Web
```

One-shot rebuild after a batch of frontend changes:

```bash
cd src/QueryPlus.Web && pnpm run build
dotnet run --project src/QueryPlus.Web
```

#### Useful ClientApp commands

```bash
cd src/QueryPlus.Web

# Preferred (Vite+: https://viteplus.dev/)
vp install
vp build
vp test
vp dev

# Equivalent with pnpm
pnpm install
pnpm run build
pnpm test
pnpm run dev
```

Skip the MSBuild ClientApp step when needed:

```bash
dotnet publish ... /p:SkipClientAppBuild=true
```

Edit styles under `ClientApp/src/styles/` (not under `wwwroot`).  
`_Layout` only loads `~/dist/js/app.js` and `~/dist/css/site.css` (no CDNs).

### 4. Run the web app

```bash
dotnet run --project src/QueryPlus.Web
```

Open the URL printed by Kestrel (typically `https://localhost:7xxx` or `http://localhost:5xxx`).

Configure Keycloak client redirect URIs to match that URL if needed (`docker/keycloak/realm-export.json`).

## Build & test

```bash
dotnet restore
dotnet build QueryPlus.sln    # builds ClientApp only if dist/js/app.js is missing
dotnet test QueryPlus.sln

# ClientApp unit tests (Vitest + jsdom)
cd src/QueryPlus.Web && pnpm test
```

For a full local frontend rebuild before test/run:

```bash
cd src/QueryPlus.Web && pnpm run build && cd ../..
dotnet test QueryPlus.sln
```

## Docker (full stack)

```bash
docker compose --profile full up --build
```

- App: http://localhost:5000  
- Uses `appsettings.Docker.json` / environment variables for SQL Server and Keycloak.

## Dev Containers

1. Open the repo in VS Code / Cursor.
2. **Dev Containers: Reopen in Container**.
3. SQL Server and Keycloak start via Compose; .NET 10 is available in the `app` service.

```bash
dotnet run --project src/QueryPlus.Web --urls http://0.0.0.0:5000
```

## Configuration

| Setting | Description |
|---------|-------------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection |
| `Keycloak:Authority` | e.g. `http://localhost:8080/realms/queryplus` |
| `Keycloak:ClientId` | `queryplus-web` |
| `Keycloak:ClientSecret` | Must match Keycloak client secret |
| `Keycloak:RequireHttpsMetadata` | `false` for local HTTP Keycloak |

Localization: `?culture=pt-BR` or `?culture=en` (also cookie / `Accept-Language`).

## Authentication notes

- OpenID Connect authorization code flow against Keycloak.
- Cookie session after login (`QueryPlus.Auth`).
- `/Account/Login` challenges OIDC; `/Account/Logout` signs out cookie + OIDC.

### Dev Containers / Docker networking

The browser must never be redirected to the Docker DNS name `keycloak`.

| Setting | Purpose |
|---------|---------|
| `Keycloak:Authority` | Public URL for the browser (`http://localhost:8080/realms/queryplus`) |
| `Keycloak:MetadataAddress` | Internal discovery URL (`http://keycloak:8080/realms/.../.well-known/...`) |
| `Keycloak:BackchannelHost` | Rewrites server token/JWKS calls from `localhost` → `keycloak` |

Keycloak is started with `KC_HOSTNAME=localhost` so issuer/authorize URLs are host-reachable.

After changing compose env vars, recreate containers:

```bash
docker compose down
docker compose up -d sqlserver keycloak
# or: Dev Containers → Rebuild Container
```

Change the client secret before any non-dev environment.

## Primary keys

All domain entities use **`int`** identity primary keys.

## Demo data (automatic on startup)

On application start, `DemoDataSeeder`:

1. Applies EF Core migrations  
2. Installs demo tables + stored procedures from `src/QueryPlus.Data/Seed/demo-objects.sql`  
3. Registers categories/procedures/parameters/columns from `demo-catalog.json` (idempotent)

### Highlights

| Object | Purpose |
|--------|---------|
| `tb_usa_president` + `Sp_USA_President_List` | Full US presidents list; combo **State**, dates **Start** / **End** |
| 30+ additional `Sp_Demo_*` procedures | FreeText, Numeric, Date, Time, DateTime, Boolean, Combo; formats & alignments |
| Supporting tables | customers, products, orders, employees, invoices, events, sensors, countries, flights, transactions |

Role entitlement for demo procedures is **`user`** (also works for `admin`).

SQL scripts are also mirrored under `docs/database/demo-objects.sql`.

## License

Proprietary / internal — adjust as needed.
