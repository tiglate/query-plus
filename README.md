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
- Optional: Node.js 18+ (rebuild Tailwind CSS)

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

### 4. Optional: Tailwind CSS rebuild

A prebuilt `wwwroot/css/site.css` is committed. To regenerate:

```bash
cd src/QueryPlus.Web
npm install
npm run build:css
# or: npm run watch:css
```

## Build & test

```bash
dotnet restore
dotnet build QueryPlus.sln
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

Change the client secret before any non-dev environment.

## Primary keys

All domain entities use **`int`** identity primary keys (`BaseEntity.Id`).

## License

Proprietary / internal — adjust as needed.
