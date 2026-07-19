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

## Layout (Phase 0)

| Path | Purpose |
|------|---------|
| `src/entries/app.ts` | Global shell entry (stub marker) |
| `src/styles/main.css` | Tailwind 4 + theme |
| `src/styles/legacy-components.css` | Temporary port of `wwwroot/css/input.css` |
| `tests/` | Vitest + jsdom |

Runtime still uses legacy `wwwroot/js/*` and `wwwroot/css/site.css` until later phases.
