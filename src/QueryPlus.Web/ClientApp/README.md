# QueryPlus ClientApp

TypeScript + Tailwind 4 sources for the Razor/HTMX UI.  
Plan: [`docs/frontend-reorganization.md`](../../../docs/frontend-reorganization.md).

## Commands (from `src/QueryPlus.Web`)

| Task | Vite+ | pnpm fallback |
|------|--------|----------------|
| Install | `vp install` | `pnpm install` |
| Build → `wwwroot/dist` | `vp build` | `pnpm run build` |
| Watch | `vp dev` | `pnpm run dev` |
| Tests | `vp test` | `pnpm test` |
| Check | `vp check` | `pnpm run check` |

## Layout

| Path | Purpose |
|------|---------|
| `src/entries/app.ts` | Global shell entry (DI bootstrap) |
| `src/core/` | DI, `PageController`, `HtmxBridge`, bootstrap |
| `src/components/` | sheet-grid, nav-dropdown, confirm-submit, parameter-combo |
| `src/pages/` | home, admin, shared controllers |
| `src/styles/` | Tailwind 4 modular CSS (`main.css` + base/components/pages) |
| `tests/` | Vitest + jsdom |

## Runtime

- `_Layout` loads only `~/dist/js/app.js` + `~/dist/css/site.css` (plus CDNs until Phase 7).
- **Page keys:** `home`, `admin-categories`, `admin-procedures`, `admin-procedure-edit`.
- No legacy `wwwroot/js` or `wwwroot/css` sources — ClientApp is the single frontend source of truth.
