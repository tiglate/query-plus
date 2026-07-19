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
| `src/components/` | nav-dropdown, confirm-submit, … |
| `src/pages/shared/` | `SharedShellController` (layout behaviors) |
| `src/styles/main.css` | Tailwind 4 + theme |
| `src/styles/legacy-components.css` | Temporary port of `wwwroot/css/input.css` |
| `tests/` | Vitest + jsdom |

### Runtime (Phases 1–5)

- `_Layout` loads `~/dist/js/app.js` + `~/dist/css/site.css`.
- **Page keys:** `home`, `admin-categories`, `admin-procedures`, `admin-procedure-edit`.
- Styles: modular Tailwind 4 under `src/styles/` (`main.css` + base/components/pages).
- `wwwroot/js/site.js`, `sheet-grid.js`, and `wwwroot/css/*` are deprecated stubs (not loaded).
