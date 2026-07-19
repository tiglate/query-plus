# Frontend reorganization plan

**Branch:** `feature/reorganize-frontend`  
**Status:** Approved — Phases 0–5 complete; next is Phase 6 (delete legacy JS)  
**Last updated:** 2026-07-19

This document is the source of truth for the TypeScript / Vite+ / DI frontend reorg. Resume from here if the session is interrupted.

---

## Goals

| Keep | Change |
|------|--------|
| Razor + HTMX (no SPA) | TypeScript + OOP page controllers |
| Clusterize + SheetGrid | Multi-entry bundles, shared core |
| Tailwind RCB palette | Modular CSS imports + **Tailwind 4** |
| Server-driven UX | Full DI (**tsyringe**) |
| Existing admin/home behavior | Vitest + jsdom unit tests |

**End state:** no CDNs (HTMX, Clusterize, Font Awesome, Google Fonts all via npm/local). Timing is flexible as long as CDNs are gone by the end.

---

## Decisions (locked)

| # | Decision |
|---|----------|
| Migration style | **Incremental** (phases; layout keeps legacy JS until replaced) |
| Bundling | **Multi-entry** (dynamic `import()` by `data-page` first; optional separate script tags later) |
| Tests | **Vitest + jsdom** |
| DI | **tsyringe** (full container) |
| CSS | **Tailwind 4** + modular CSS imports under `ClientApp/src/styles/` |
| HTMX / Clusterize | Stay; CDN short-term → npm/local in Phase 7 |
| Toolchain | **Vite+** (`vp`) with **pnpm** + `package.json` script fallbacks |
| `wwwroot/dist` | **gitignore**; build in Docker / `dotnet publish` |
| Page keys | `home`, `admin-categories`, `admin-procedures`, `admin-procedure-edit`, … |

### pnpm + Vite+

- Set `"packageManager": "pnpm@…"` in `src/QueryPlus.Web/package.json`.
- Use `pnpm-lock.yaml` (drop npm lockfile for the web client).
- Preferred: `vp install` / `vp run build` / `vp test` / `vp check`.
- Fallback: `pnpm install` / `pnpm run build` / `pnpm test`.
- Docker/CI: Corepack or install pnpm → `pnpm install --frozen-lockfile && pnpm run build` before publish.

### Tailwind 4

- Prefer `@tailwindcss/vite` (or Vite+ CSS path) over the old Tailwind 3 CLI.
- Theme via CSS `@theme { … }` (navy / cyan / lime / leaf, Inter).
- Scan Razor (`**/*.cshtml`) and ClientApp TS so utilities are not purged.
- Modular files under `ClientApp/src/styles/`; single built CSS in `wwwroot/dist/css/`.

---

## Target layout

```
src/QueryPlus.Web/
  ClientApp/
    src/
      core/                 # DI, bootstrap, PageController, HtmxBridge
      components/           # sheet-grid, nav-dropdown, confirm-submit, …
      pages/
        shared/
        home/
        admin/
          categories/
          procedures/
      styles/
        main.css            # @import chain + Tailwind 4
        base/
        components/
        pages/
      entries/
        app.ts              # global shell (always loaded)
        home.ts             # optional dedicated entry later
        admin-*.ts
    tests/                  # Vitest + jsdom (mirrors src/)
  wwwroot/
    dist/                   # Vite build output (gitignored)
      js/
      css/
    js/                     # legacy site.js + sheet-grid.js until Phase 6
    css/                    # legacy input.css + site.css until layout switches
  vite.config.ts
  package.json
  pnpm-lock.yaml
  tsconfig.json
```

### Razor wiring (target)

```html
<link rel="stylesheet" href="~/dist/css/site.css" asp-append-version="true" />
<script type="module" src="~/dist/js/app.js" asp-append-version="true"></script>
<body data-page="home">
```

**Bootstrap strategy (preferred first):** one layout script (`app.js`) that reads `data-page` and dynamically imports the page module (code-split multi-entry). Optional later: separate `<script>` tags per page for stricter loading.

---

## Architecture

### Page controllers (OOP)

```ts
abstract class PageController {
  abstract mount(root: ParentNode): void;
  unmount(): void {}
  dispose(): void { this.unmount(); }
}

@injectable()
class HomePageController extends PageController {
  constructor(
    private readonly sheetGrid: SheetGridService,
    private readonly htmx: HtmxBridge,
  ) { super(); }

  mount(root: ParentNode): void { /* … */ }
}
```

### Full DI (tsyringe)

- Tokens for DOM-bound services (`SheetGridService`, `HtmxBridge`, `StorageService`).
- Controllers resolved from container; no bare `new` for infrastructure in pages.
- Tests rebind tokens with fakes.

### Components

- `SheetGrid` → DI service (`mount` / `destroy` / `refresh`).
- Temporary `window.QueryPlusSheetGrid` bridge during migration.
- Nav dropdown, confirm-on-submit, combo visibility as small services/components.

### HTMX

- Stay on HTMX 2.x.
- Central `HtmxBridge` for `afterSwap` / `beforeSwap` / `configRequest`.
- CSRF meta handling in app bootstrap.

### Explicit non-goals

- No React/Vue/Svelte SPA, no Blazor.
- No replacing HTMX with a fetch-only mini-SPA.
- No RCB palette / home layout redesign in this workstream.

---

## CDN → npm roadmap

| Asset | Now | Intermediate | End |
|-------|-----|--------------|-----|
| HTMX | unpkg CDN | keep CDN | `htmx.org` npm |
| Clusterize | cdnjs | keep CDN + global | npm or vendored |
| Font Awesome | cdnjs | keep CDN | `@fortawesome/fontawesome-free` local |
| Inter font | Google Fonts | keep or system | self-host `woff2` |
| jQuery validation | local `wwwroot/lib` | leave | optional later |

**When:** after core TS migration is stable (dedicated Phase 7 PR preferred).

---

## Phases

### Phase 0 — Scaffold (no behavior change)

- [x] Vite+ + pnpm + `ClientApp/` + tsconfig
- [x] Multi-entry/build → `wwwroot/dist` (stub `app.js` + marker)
- [x] Tailwind 4 pipeline (`@tailwindcss/vite`, `@theme`, legacy components import)
- [x] Vitest + jsdom smoke test
- [x] gitignore `wwwroot/dist`
- [x] MSBuild / Docker: Node + pnpm build before publish
- [x] README: `vp` / `pnpm` install, build, test, dev
- [x] Layout still uses existing `site.js` / CDNs (no runtime behavior change)

### Phase 1 — Core shell + DI

- [x] Container (tsyringe), `PageController`, bootstrap, `HtmxBridge`
- [x] Migrate nav dropdown + confirm submit (+ Escape/focusin fix)
- [x] Load `app.js` in layout; remove duplicate site.js/CSRF handlers
- [x] Unit tests for nav delay, confirm cancel/allow, CSRF meta

### Phase 2 — SheetGrid component

- [x] Port `sheet-grid.js` → TS + DI (`SheetGrid` / `SheetGridService`)
- [x] Temporary global re-export via `installGlobalBridge()` (layout drops `sheet-grid.js` script)
- [x] Tests: pure helpers, mount/destroy with Clusterize mock
- [x] Fix home `.qp-sheet-grid--home` flex height chain (vertical scrollbar)

### Phase 3 — Home page controller

- [x] Extract home logic from `site.js` → `HomePageController` + helpers
- [x] `data-page="home"` / `ViewData["PageKey"]` + layout `body[data-page]`
- [x] Tests for execute/export/required-param guards + pure helpers
- [x] `site.js` reduced to admin-only (combo visibility, sync metadata)

### Phase 4 — Admin pages

- [x] Combo visibility + sync-metadata (`AdminProcedureFormController`)
- [x] Admin list sheet-grid mount (`AdminListPageController`)
- [x] `data-page`: `admin-categories`, `admin-procedures`, `admin-procedure-edit`
- [x] Remove `site.js` from layout (stub remains until Phase 6)

### Phase 5 — CSS modularization + layout switch

- [x] Split styles under `ClientApp/src/styles/**` (base, components, pages)
- [x] Layout points at `~/dist/css/site.css` (Tailwind 4 via Vite)
- [x] Media queries + home sheet flex rules verified in dist output
- [x] Legacy `wwwroot/css/*` deprecated (no longer linked)

### Phase 6 — Delete legacy JS

- [ ] Remove `wwwroot/js/site.js`, `sheet-grid.js`
- [ ] Only `dist/` + layout script tags

### Phase 7 — De-CDN

- [ ] npm: htmx, clusterize (or vendor), fontawesome, self-host Inter
- [ ] Grep layout for remote asset URLs → none
- [ ] Docker/CI updated

---

## Tooling commands

```bash
cd src/QueryPlus.Web

# Preferred (Vite+)
vp install
vp dev          # watch TS + CSS → wwwroot/dist
vp build        # production
vp test
vp check        # lint + format + types (when configured)

# Fallback (pnpm)
pnpm install
pnpm run dev
pnpm run build
pnpm test
pnpm run check
```

### .NET / Docker

- **Dev:** run `vp dev` or `pnpm run dev` alongside `dotnet run`.
- **Publish:** MSBuild target runs `pnpm install --frozen-lockfile && pnpm run build` when Node is available.
- **Dockerfile:** Node + pnpm in build stage; build ClientApp before `dotnet publish`.

---

## Current baseline (pre-reorg)

| Asset | Role |
|-------|------|
| `wwwroot/js/site.js` (~550 LOC) | Home + admin + layout god script |
| `wwwroot/js/sheet-grid.js` (~466 LOC) | Clusterize sheet grid |
| `wwwroot/css/input.css` (~716 LOC) | All custom components (Tailwind 3) |
| CDNs | HTMX, Clusterize, FA, Google Fonts |
| Tooling (before Phase 0) | Tailwind 3 CLI only |

---

## Resume checklist

1. Read this file and tick completed phase items.
2. `git status` on `feature/reorganize-frontend`.
3. Continue the first unchecked phase; do not skip to de-CDN early unless necessary.
4. Prefer small PRs per phase (or 0+1 combined if tiny).

---

## Residual notes

- Vite+ is relatively new; if `vp` misbehaves, keep the same folder layout and use plain Vite + Vitest + pnpm scripts.
- Bootstrap under `wwwroot/lib` appears unused by layout (Tailwind UI) — confirm and drop later if true.
- Fallback if Tailwind 4 + Vite+ friction blocks Phase 0: Vite + `@tailwindcss/vite` without abandoning v4.
`)