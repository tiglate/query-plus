export const QUERYPLUS_CLIENT_VERSION = "0.7.0-phase7";

/** Side-effect marker so production bundles stay non-empty (CSS is co-emitted). */
export function markClientAppLoaded(target: ParentNode = document): void {
  const root = target instanceof Document ? target.documentElement : (target as Element);
  if ("setAttribute" in root) {
    root.setAttribute("data-qp-client", QUERYPLUS_CLIENT_VERSION);
  }
}
