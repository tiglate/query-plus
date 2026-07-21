/**
 * Global ClientApp shell entry.
 * Vendors (htmx, clusterize) + DI bootstrap + page controllers.
 * Font Awesome / Inter / Clusterize CSS load via styles/main.css.
 */
import "reflect-metadata";
import "@/styles/main.css";
import { markClientAppLoaded, QUERYPLUS_CLIENT_VERSION } from "@/clientMeta";
import { bootstrap } from "@/core/bootstrap";

export { markClientAppLoaded, QUERYPLUS_CLIENT_VERSION };

/**
 * Guard against double evaluation of the entry module.
 *
 * ASP.NET MapStaticAssets may serve the entry as a fingerprinted URL
 * (e.g. app.xxxxx.js) while an async vendor chunk still imports "./app.js".
 * Those are two module instances; without a global guard each would mount
 * click handlers twice and Maximize would toggle on→off in one click.
 */
const BOOT_FLAG = "__qpClientBootstrapped";

type BootGlobal = typeof globalThis & { [BOOT_FLAG]?: boolean };

async function start(): Promise<void> {
  const g = globalThis as BootGlobal;
  if (g[BOOT_FLAG]) return;
  g[BOOT_FLAG] = true;

  // Dynamic import keeps Vitest free of htmx's jsdom XPath init.
  await import("@/vendor");
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
