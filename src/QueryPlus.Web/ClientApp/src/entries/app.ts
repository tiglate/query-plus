/**
 * Global ClientApp shell entry.
 * Vendors (htmx, clusterize) + DI bootstrap + page controllers.
 * Font Awesome / Inter / Clusterize CSS load via styles/main.css.
 */
import "reflect-metadata";
import "../styles/main.css";
import {
  markClientAppLoaded,
  QUERYPLUS_CLIENT_VERSION,
} from "../clientMeta";
import { bootstrap } from "../core/bootstrap";

export { markClientAppLoaded, QUERYPLUS_CLIENT_VERSION };

async function start(): Promise<void> {
  // Dynamic import keeps Vitest free of htmx's jsdom XPath init.
  await import("../vendor");
  markClientAppLoaded(document);
  bootstrap();
}

// Skip auto-start under Vitest (tests import symbols from this module).
if (typeof document !== "undefined" && import.meta.env.MODE !== "test") {
  if (document.readyState === "loading") {
    document.addEventListener(
      "DOMContentLoaded",
      () => {
        void start();
      },
      { once: true },
    );
  } else {
    void start();
  }
}
