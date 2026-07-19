import { getAppContainer, configureContainer } from "./di/container";
import { SharedShellController } from "../pages/shared/SharedShellController";

export interface BootstrapOptions {
  /** Document root; defaults to global document. */
  document?: Document;
  /** When true, also resolve data-page controllers (Phase 3+). */
  enablePageControllers?: boolean;
}

/**
 * Configure DI and mount the shared shell (layout behaviors).
 * Page-specific controllers are added in later phases via data-page.
 */
export function bootstrap(options: BootstrapOptions = {}): SharedShellController {
  configureContainer(
    options.document ? { document: options.document } : undefined,
  );
  const c = getAppContainer();
  const shell = c.resolve(SharedShellController);
  const root = options.document ?? document;
  shell.mount(root);

  // Phase 3+: read data-page and dynamically import page modules.
  if (options.enablePageControllers) {
    // reserved
  }

  return shell;
}
