# QueryPlus ClientApp

TypeScript + Tailwind 4 sources for the ASP.NET Core MVC / HTMX UI.

## Commands (from `src/QueryPlus.Web`)

| Task                   | Vite+        | pnpm fallback    |
| ---------------------- | ------------ | ---------------- |
| Install                | `vp install` | `pnpm install`   |
| Build → `wwwroot/dist` | `vp build`   | `pnpm run build` |
| Watch                  | `vp dev`     | `pnpm run dev`   |
| Tests                  | `vp test`    | `pnpm test`      |
| Check                  | `vp check`   | `pnpm run check` |

## Layout

| Path                 | Purpose                                                          |
| -------------------- | ---------------------------------------------------------------- |
| `src/entries/app.ts` | Global shell entry (DI bootstrap)                                |
| `src/core/`          | DI, `PageController`, `HtmxBridge`, client validation, bootstrap |
| `src/components/`    | sheet-grid, nav-dropdown, confirm-submit, parameter-combo        |
| `src/pages/`         | home, admin, shared page controllers                             |
| `src/styles/`        | Tailwind 4 modular CSS (`main.css` + base/components/pages)      |
| `tests/`             | Vitest + jsdom                                                   |

## Runtime

- Layout loads only `~/dist/js/app.js` + `~/dist/css/site.css` (no CDNs).
- Vendors (npm): `htmx.org`, `clusterize.js`, `@fortawesome/fontawesome-free`, `@fontsource/inter`.
- **Page keys:** `home`, `admin-categories`, `admin-procedures`, `admin-procedure-edit`.
- ClientApp is the single frontend source of truth.

See the repository [README](../../../README.md) for full setup and [SPECIFICATION](../../../docs/SPECIFICATION.md) for product requirements.
