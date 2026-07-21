import { SheetGridService } from "@/components/sheet-grid/SheetGridService";
import { AdminListPageController } from "@/pages/admin/AdminListPageController";
import { AdminProcedureFormController } from "@/pages/admin/AdminProcedureFormController";
import { HomePageController } from "@/pages/home/HomePageController";
import { ResultsMaximize } from "@/pages/home/ResultsMaximize";
import { SharedShellController } from "@/pages/shared/SharedShellController";
import { configureContainer, getAppContainer } from "./di/container";
import type { PageController } from "./PageController";
import { resolvePageKey } from "./pageKey";

export { resolvePageKey } from "./pageKey";

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
 * Configure DI, install sheet-grid global bridge, mount shared shell + page controller.
 */
export function bootstrap(options: BootstrapOptions = {}): BootstrapResult {
    configureContainer(options.document ? { document: options.document } : undefined);
    const c = getAppContainer();
    const doc = options.document ?? document;

    // Global API for any remaining callers / future partial scripts.
    c.resolve(SheetGridService).installGlobalBridge();

    const shell = c.resolve(SharedShellController);
    shell.mount(doc);

    let page: PageController | null = null;
    const pageKey = resolvePageKey(doc);
    const enablePages = options.enablePageControllers !== false;

    if (enablePages) {
        page = resolvePageController(pageKey, c);
        page?.mount(doc);
    }

    // Belt-and-suspenders: wire Maximize whenever the control exists, even if
    // page-key resolution failed (home still needs this control).
    if (doc.getElementById("btn-toggle-results-max")) {
        c.resolve(ResultsMaximize).mount();
    }

    return { shell, page, pageKey };
}

function resolvePageController(
    pageKey: string,
    c: ReturnType<typeof getAppContainer>,
): PageController | null {
    switch (pageKey) {
        case "home":
            return c.resolve(HomePageController);
        case "admin-categories":
        case "admin-procedures":
            return c.resolve(AdminListPageController);
        case "admin-procedure-edit":
            return c.resolve(AdminProcedureFormController);
        default:
            return null;
    }
}
