# Query Plus - Software Requirements Specification

**Version:** 1.3  
**Date:** July 18, 2026  
**Status:** Final for Implementation  
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
- Support large result sets with efficient export.

## 2. Functional Requirements

### 2.1 Authentication & Authorization

- Integration with Keycloak using OpenID Connect.
- All endpoints protected.
- Each Procedure linked to one or more Roles/Entitlements.
- Menu items and procedure lists filtered according to user claims.

### 2.2 Global Layout

- Header: Logo + Application name
- Top navigation (dropdown style):
  - Home
  - Admin
    - Categories
    - Procedures
  - Support (static page with helpdesk link, phone, email, working hours)
- Footer: Copyright and basic info
- Desktop-first UI optimized for 24–27" Full HD monitors. Full use of horizontal space. No wasteful centering.

### 2.3 Execution Screen (Home)

**Components:**

- Toolbar with title and buttons: **Execute**, **Clear**, **Export to Excel**
- Combo box listing accessible procedures
- Textarea displaying the procedure Description
- Left panel: Dynamically generated parameter input controls (vertical layout)
- Right panel: Paginated results grid (initially empty)
- Client-side column sorting on displayed results

**Parameter Input Rules:**

- Free Text → text input
- Numeric → number input
- Date / Time / DateTime → appropriate HTML controls
- Boolean → checkbox/switch
- Combo → select with predefined options

### 2.4 Categories Management

- Search screen with filters and grid (ID, Description, Created At, Updated At)
- Add/Edit/View/Delete operations
- Edit/View shows Audit section (ID, Created/Updated By/At)

### 2.5 Procedures Management

**Search Screen**

- Filters (Category, Caption, Role, Enabled, etc.)
- Grid with actions: View, Edit, Delete (confirmation required)

**Add/Edit Screen**
**Main Fields:**

- Category (dropdown)
- Caption (user-friendly name)
- Database Name
- Procedure Name (technical)
- Enabled (boolean switch)
- Role / Entitlement
- Description (textarea, max 500 characters)

**Parameters Section (Master/Detail)**

- Add/Edit/Delete parameters
- Fields per parameter: Caption, Name (@paramName), Type (FreeText, Numeric, Date, Time, DateTime, Boolean, Combo), Default Value, Combo Values (JSON array for Combo type)

**Columns Section (Master/Detail)**

- Add/Edit/Delete columns
- Fields per column: Technical Name, Caption, Alignment (Left/Center/Right), Format Mask (e.g. {0:n2}), Visible (bit)

**Additional Features:**

- "Sync Metadata" button to load parameters and columns from SQL Server system views
- Audit fields visible on Edit/View
- Atomic save of main record + details

### 2.6 Server-side pagination (heavy procedures)

- Procedure catalog flag: **Supports pagination** (`supports_pagination`).
- Contract (fixed, non-customizable SP arguments — never exposed as user parameters):
  - `@PageNumber BIGINT = 1`
  - `@PageSize BIGINT = 50`
  - `@TotalRecords BIGINT OUTPUT`
- Home execute injects page args when the flag is set; the results grid shows a server-side pager.
- Metadata sync and validation reject cataloging reserved pagination parameter names.
- ADO.NET command timeout for stored procedures: **30 minutes**.

### 2.7 Excel Export

- For large datasets, execution is queued to a background worker.
- User receives progress feedback (persists across refresh via polling)
- Notification + download link when file is ready
- For paginated procedures, export re-executes with `@PageNumber = 1` and `@PageSize = 999,999,999` so the full dataset is exported (not only the current page).

### 2.8 Auditing & Logging

- **Execution Log**: username, IP address, execution timestamps, procedure, parameter values (JSON), row count, success flag, error message
- **Configuration Audit**: Full history (\_aud tables) for Categories, Procedures, Parameters and Columns using revision pattern
- Configurable log levels
- Email notification on errors (deduplication mechanism required)

### 2.9 Internationalization

- Default: Brazilian Portuguese (pt-BR)
- Secondary: English (en)
- User can switch language easily

## 3. Technical Requirements

### 3.1 Stack

- Backend: .NET 10, ASP.NET Core MVC (Controllers + Views) + HTMX
- Data: EF Core (CRUD + Migrations), ADO.NET for dynamic result sets from stored procedures
- Styling: Tailwind CSS
- Auth: Keycloak
- Database: Microsoft SQL Server
- Development: Dev Containers (Rider / VS Code on Linux)
- Production: IIS on Windows Server

### 3.2 Architecture

- Clean/Layered Architecture:
  - Domain
  - Application (Services, DTOs, Interfaces)
  - Infrastructure
  - Data (EF Core)
  - Web (Razor Pages)

### 3.3 Database

Refer to `docs/database/schema.sql` for the complete schema (INT keys, tb\_ prefix, audit tables, execution log, etc.).

## 4. Non-Functional Requirements

- Secure parameterized execution only
- Server-side pagination on procedure results
- No heavy client-side frameworks
- Excellent desktop UX
- Comprehensive unit testing + Testcontainers support
- Easy local development with containers

## 5. Assumptions

- SQL Server has Cross-Database Ownership Chaining enabled
- Stored procedures return a single tabular result set
- Procedures are designed with server-side pagination where applicable

---

This document, together with `docs/database/schema.sql`, serves as the **single source of truth** for all implementation decisions.
