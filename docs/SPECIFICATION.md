# Query Plus - Software Requirements Specification

**Version:** 1.4  
**Date:** July 20, 2026  
**Status:** Implemented baseline  
**Audience:** Developers, Architects, and Reviewers

## 1. Introduction

### 1.1 Purpose

Query Plus is a secure, governed web application that allows business users to discover and execute predefined SQL Server stored procedures autonomously.

The system replaces manual query execution by the Production team, reducing risk of errors, performance issues, data leaks, and unauthorized modifications while implementing modern governance and security practices.

### 1.2 Objectives

- Empower business users to run approved queries independently.
- Centralize and version-control stored procedure metadata.
- Enforce RBAC at procedure level.
- Provide auditable execution with full traceability.
- Support large result sets with efficient export and server-side pagination.

## 2. Functional Requirements

### 2.1 Authentication & Authorization

- Integration with Keycloak using OpenID Connect (authorization code flow).
- Cookie session after login (`QueryPlus.Auth`); logout is antiforgery-protected.
- All application endpoints require authentication by default (`[AllowAnonymous]` only where needed).
- Each Procedure is linked to one or more Roles/Entitlements (claims).
- Menu items and procedure lists are filtered according to user roles.

### 2.2 Global Layout

- Header: Logo + application name, user identity, language switch, theme control (Light / Dark / System), login/logout
- Top navigation (dropdown style):
  - Home
  - Admin
    - Categories
    - Procedures
  - Support (static helpdesk contact info)
- Footer: copyright, tagline, optional database connection display
- Desktop-first UI optimized for 24–27" Full HD monitors; full use of horizontal space

### 2.3 Execution Screen (Home)

**Components:**

- Toolbar: **Execute**, **Clear**, **Export to Excel**
- Outlook-style procedure list grouped by category (caption + description)
- Parameter panel: dynamically generated inputs from catalog metadata
- Results panel: virtualized grid (Clusterize); optional maximize
- Server-side pager when the procedure supports pagination
- Client-side column reorder / resize on the current page of results

**Parameter Input Rules:**

- Free Text → text input  
- Numeric → number input  
- Date / Time / DateTime → appropriate HTML controls  
- Boolean → checkbox  
- Combo → select with predefined options  
- Required parameters validated client- and server-side  

**Reserved pagination parameters** (`@PageNumber`, `@PageSize`, `@TotalRecords`) are never shown as user fields.

### 2.4 Categories Management

- Search screen with filters, page size, and results grid (ID, Description, Created At, Updated At)
- Card layout: results body (edge-to-edge grid) + footer pager
- Add / Edit / Details / Delete (confirmation required)
- Edit/Details show audit timestamps

### 2.5 Procedures Management

**Search Screen**

- Filters (Category, Caption, Role, Enabled, page size)
- Grid actions: View (Details), Edit, Delete (confirmation required)
- Card body/footer pager pattern (same as Categories)

**Add / Edit Screen — main fields**

- Category (dropdown)
- Caption (user-friendly name)
- Database Name
- Procedure Name (technical, optionally schema-qualified)
- Enabled
- **Supports pagination** (`supports_pagination`)
- Role / Entitlement
- Description (textarea, max 500 characters)

**Parameters section (master/detail)**

- Add / remove parameters
- Fields: Caption, Name (`@paramName`), Type (FreeText, Numeric, Date, Time, DateTime, Boolean, Combo), Default Value, Combo Values (JSON array), Is Required  
- Reserved pagination names are rejected by validation and stripped on metadata sync

**Columns section (master/detail)**

- Add / remove columns
- Fields: Technical Name, Caption, Alignment (Left/Center/Right), Format Mask (e.g. `{0:n2}`), Visible

**Additional features**

- **Sync metadata** — load parameters/columns from SQL Server system views
- Atomic save of main record + details
- Details screen is read-only

### 2.6 Server-side pagination (heavy procedures)

- Catalog flag: **Supports pagination** (`supports_pagination` on `tb_procedure`).
- Fixed SP contract (not user-configurable catalog parameters):
  - `@PageNumber BIGINT = 1`
  - `@PageSize BIGINT = 50`
  - `@TotalRecords BIGINT OUTPUT`
- When the flag is set, Home injects page args and reads total from OUTPUT; the results card shows a server-side pager.
- UI page size is capped (interactive); export uses a giant page size (see Excel Export).
- ADO.NET command timeout for stored procedures: **30 minutes**.

### 2.7 Excel Export

- Export is queued to a background worker after a successful execute with data.
- Eligibility is tied to the last successful execute (procedure + parameter values; TTL applies).
- Progress/status polling; download link when ready (`/exports/download/{jobId}`).
- For paginated procedures, export re-executes with `@PageNumber = 1` and `@PageSize = 999,999,999` so the full dataset is exported (not only the current grid page).

### 2.8 Auditing & Logging

- **Execution log**: username, IP, start/end, procedure, parameter values (JSON), row count, success, error message
- **Configuration audit**: `*_aud` tables for Categories, Procedures, Parameters, and Columns (revision pattern)
- Configurable log levels
- Email notification on errors (deduplication) — planned / future

### 2.9 Internationalization

- Default: Brazilian Portuguese (`pt-BR`)
- Secondary: English (`en`)
- User can switch language (cookie / query string / `Accept-Language`)

### 2.10 Theme

- Light / Dark / System preference persisted in the browser
- Applied before first paint to avoid flash of incorrect theme

## 3. Technical Requirements

### 3.1 Stack

- Backend: .NET 10, ASP.NET Core **MVC (Controllers + Views)** + HTMX
- Frontend assets: TypeScript ClientApp (Vite/pnpm), Tailwind CSS 4, Clusterize, Font Awesome, Inter (no CDNs in production layout)
- Data: EF Core (CRUD + Migrations), ADO.NET/Dapper for dynamic SP result sets
- Auth: Keycloak (OpenID Connect)
- Database: Microsoft SQL Server
- Development: Dev Containers / Docker Compose
- Production target: IIS on Windows Server (or container host)

### 3.2 Architecture

Clean / layered architecture:

- Domain  
- Application (services, DTOs, validators)  
- Infrastructure (composition for external concerns)  
- Data (EF Core, Dapper executor, seed)  
- Web (MVC Controllers + Views, ClientApp, OIDC adapters)

Web host composition is a thin `Program.cs` plus `DependencyInjection/`, `Hosting/`, and `Auth/` modules. UI screens live under `Controllers/` and `Views/`.

### 3.3 Database

Refer to `docs/database/schema.sql` for the reference schema (INT keys, `tb_` prefix, audit tables, execution log, `supports_pagination`, etc.). Runtime schema is applied via EF Core migrations.

## 4. Non-Functional Requirements

- Secure parameterized execution only (no free-form SQL from the UI)
- Server-side pagination where procedures opt in
- No heavy SPA frameworks; progressive enhancement with HTMX
- Strong desktop UX for dense data grids
- Automated tests (application unit tests + web unit/integration tests; ClientApp Vitest)
- Easy local development with containers

## 5. Assumptions

- SQL Server allows executing catalogued procedures (including cross-database where configured)
- Stored procedures return a single tabular result set for grid display
- Procedures that set **Supports pagination** implement the standard paging contract

## 6. License

The application is distributed under the **MIT License** (see repository root `LICENSE`).

---

This document, together with `docs/database/schema.sql` and the EF migrations under `src/QueryPlus.Data/Migrations`, describes the intended product baseline for Query Plus.
