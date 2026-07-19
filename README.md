# QueryPlus

QueryPlus is a .NET 10 web application for managing and executing dynamic queries/stored procedures. It follows **clean architecture** with Razor Pages, HTMX, Tailwind CSS, EF Core, Dapper, and Keycloak (OpenID Connect).

**Default language:** Brazilian Portuguese (`pt-BR`), with English (`en`) support.

## Solution structure

```
QueryPlus.sln
src/
  QueryPlus.Web              # Razor Pages + HTMX + Tailwind + Keycloak OIDC
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
- Optional (ClientApp / CSS): [Node.js 22+](https://nodejs.org/) + [pnpm](https://pnpm.io/) 10+ (or [Vite+](https://viteplus.dev/) `vp`)

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

### 3. Run the web app

```bash
dotnet run --project src/QueryPlus.Web
```

Open the URL printed by Kestrel (typically `https://localhost:7xxx` or `http://localhost:5xxx`).

Configure Keycloak client redirect URIs to match that URL if needed (`docker/keycloak/realm-export.json`).

### 4. ClientApp (Vite+ / Tailwind 4)

Frontend TypeScript and Tailwind 4 live under `src/QueryPlus.Web/ClientApp/`.  
See **[docs/frontend-reorganization.md](docs/frontend-reorganization.md)** for the full migration plan.

**Phase 0 note:** the running app still loads legacy `wwwroot/js/site.js`, `sheet-grid.js`, and `wwwroot/css/site.css` (plus CDNs). The new pipeline builds to `wwwroot/dist/` (gitignored) and is not wired into `_Layout` yet.

```bash
cd src/QueryPlus.Web

# Preferred (Vite+ CLI: https://viteplus.dev/)
vp install
vp build          # → wwwroot/dist/{js,css}
vp test
vp dev            # watch mode

# Fallback (pnpm)
pnpm install
pnpm run build
pnpm test
pnpm run dev
```

`dotnet publish` runs `pnpm install --frozen-lockfile && pnpm run build` automatically (skip with `/p:SkipClientAppBuild=true`).

Legacy Tailwind 3 CSS (until Phase 5 layout switch) is still in `wwwroot/css/site.css`. Prefer editing `ClientApp/src/styles/` going forward.

## Build & test

```bash
dotnet restore
dotnet build QueryPlus.sln
dotnet test QueryPlus.sln

# ClientApp unit tests (Vitest + jsdom)
cd src/QueryPlus.Web && pnpm test
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
