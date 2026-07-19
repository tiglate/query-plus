import { container, type DependencyContainer } from "tsyringe";
import { HtmxBridge } from "../HtmxBridge";
import { ConfirmSubmitService } from "../../components/confirm-submit/ConfirmSubmitService";
import { NavDropdownService } from "../../components/nav-dropdown/NavDropdownService";
import { SheetGridService } from "../../components/sheet-grid/SheetGridService";
import { SharedShellController } from "../../pages/shared/SharedShellController";
import { TOKENS, type ConfirmFn } from "./tokens";

let configured = false;

/**
 * Register default bindings for the browser runtime.
 * Safe to call more than once (no-ops after first configure).
 */
export function configureContainer(
  overrides?: {
    document?: Document;
    window?: Window;
    confirmFn?: ConfirmFn;
  },
): DependencyContainer {
  if (!configured) {
    container.register(TOKENS.Document, {
      useValue: overrides?.document ?? globalThis.document,
    });
    container.register(TOKENS.Window, {
      useValue: overrides?.window ?? globalThis.window,
    });
    container.register(TOKENS.ConfirmFn, {
      useValue:
        overrides?.confirmFn ??
        ((message: string) => globalThis.confirm(message)),
    });

    container.registerSingleton(HtmxBridge);
    container.registerSingleton(NavDropdownService);
    container.registerSingleton(ConfirmSubmitService);
    container.registerSingleton(SheetGridService);
    container.registerSingleton(SharedShellController);
    configured = true;
  } else if (overrides) {
    // Test re-bind path: create a child container instead of mutating global mid-flight.
    return createTestContainer(overrides);
  }

  return container;
}

/** Fresh container for unit tests (does not touch the singleton app container). */
export function createTestContainer(overrides?: {
  document?: Document;
  window?: Window;
  confirmFn?: ConfirmFn;
}): DependencyContainer {
  const child = container.createChildContainer();
  child.register(TOKENS.Document, {
    useValue: overrides?.document ?? globalThis.document,
  });
  child.register(TOKENS.Window, {
    useValue: overrides?.window ?? globalThis.window,
  });
  child.register(TOKENS.ConfirmFn, {
    useValue:
      overrides?.confirmFn ?? ((message: string) => globalThis.confirm(message)),
  });
  child.registerSingleton(HtmxBridge);
  child.registerSingleton(NavDropdownService);
  child.registerSingleton(ConfirmSubmitService);
  child.registerSingleton(SheetGridService);
  child.registerSingleton(SharedShellController);
  return child;
}

export function getAppContainer(): DependencyContainer {
  if (!configured) {
    configureContainer();
  }
  return container;
}

/** Reset flag for tests that need a clean global container. */
export function resetContainerConfiguration(): void {
  configured = false;
  container.clearInstances();
  container.reset();
}
