import { SheetGridService } from "../components/sheet-grid/SheetGridService";
import { HomePageController } from "../pages/home/HomePageController";
import { SharedShellController } from "../pages/shared/SharedShellController";
import { configureContainer, getAppContainer } from "./di/container";
import type { PageController } from "./PageController";

export interface BootstrapOptions {
  /** Document root; defaults to global document. */
  document?: Document;
  /** Mount data-page controllers (default true). */
  enablePageControllers?: boolean;
}

export interface BootstrapResult {
  shell: SharedShellController;
  page: PageController | null;
  pageKey: string;
}

/**
 * Resolve page key from body[data-page] or first [data-page] in the document.
 */
export function resolvePageKey(doc: Document = document): string {
  const fromBody = doc.body?.getAttribute("data-page");
  if (fromBody) return fromBody;
  return doc.querySelector("[data-page]")?.getAttribute("data-page") || "";
}

/**
 * Configure DI, install sheet-grid global bridge, mount shared shell + page controller.
 */
export function bootstrap(
  options: BootstrapOptions = {},
): BootstrapResult {
  configureContainer(
    options.document ? { document: options.document } : undefined,
  );
  const c = getAppContainer();
  const doc = options.document ?? document;

  // site.js / admin @section Scripts still call window.QueryPlusSheetGrid.
  c.resolve(SheetGridService).installGlobalBridge();

  const shell = c.resolve(SharedShellController);
  shell.mount(doc);

  let page: PageController | null = null;
  const pageKey = resolvePageKey(doc);
  const enablePages = options.enablePageControllers !== false;

  if (enablePages && pageKey === "home") {
    page = c.resolve(HomePageController);
    page.mount(doc);
  }

  return { shell, page, pageKey };
}
