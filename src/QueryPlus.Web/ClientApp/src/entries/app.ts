/**
 * Global ClientApp shell entry.
 * Phase 0: loads stylesheet pipeline only (no behavior change).
 * Later phases: DI bootstrap + page controllers via data-page.
 */
import "../styles/main.css";

export const QUERYPLUS_CLIENT_VERSION = "0.0.0-phase0";

/** Side-effect marker so production bundles stay non-empty (CSS is co-emitted). */
export function markClientAppLoaded(target: ParentNode = document): void {
  const root = target instanceof Document ? target.documentElement : (target as Element);
  if ("setAttribute" in root) {
    root.setAttribute("data-qp-client", QUERYPLUS_CLIENT_VERSION);
  }
}

// Always run: proves the entry is wired when/if layout loads dist/js/app.js.
if (typeof document !== "undefined") {
  markClientAppLoaded(document);
}
