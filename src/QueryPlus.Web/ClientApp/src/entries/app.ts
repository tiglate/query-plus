/**
 * Global ClientApp shell entry.
 * Phase 1: DI bootstrap + shared shell (nav dropdown, confirm submit, CSRF).
 * Later: data-page controllers.
 */
import "reflect-metadata";
import "../styles/main.css";
import { bootstrap } from "../core/bootstrap";

export const QUERYPLUS_CLIENT_VERSION = "0.1.0-phase1";

/** Side-effect marker so production bundles stay non-empty (CSS is co-emitted). */
export function markClientAppLoaded(target: ParentNode = document): void {
  const root =
    target instanceof Document ? target.documentElement : (target as Element);
  if ("setAttribute" in root) {
    root.setAttribute("data-qp-client", QUERYPLUS_CLIENT_VERSION);
  }
}

function start(): void {
  markClientAppLoaded(document);
  bootstrap();
}

// Skip auto-start under Vitest (tests import symbols from this module).
if (typeof document !== "undefined" && import.meta.env.MODE !== "test") {
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", start, { once: true });
  } else {
    start();
  }
}

