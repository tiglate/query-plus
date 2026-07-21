import { inject, injectable, singleton } from "tsyringe";
import { HtmxBridge } from "@/core/HtmxBridge";
import { PageController } from "@/core/PageController";
import { TOKENS } from "@/core/di/tokens";
import {
  canExport,
  formatRequiredParamsMessage,
  isParamFieldName,
  isValidProcedureId,
} from "./homeGuards";
import { HomeResultsService } from "./HomeResultsService";
import { ResultsMaximize } from "./ResultsMaximize";

/**
 * Main execution screen: procedure list, execute/export guards, results grid, maximize.
 */
@singleton()
@injectable()
export class HomePageController extends PageController {
  private readonly disposers: Array<() => void> = [];
  private mounted = false;

  constructor(
    @inject(TOKENS.Document) private readonly doc: Document,
    @inject(TOKENS.Window) private readonly win: Window,
    @inject(HtmxBridge) private readonly htmx: HtmxBridge,
    @inject(HomeResultsService) private readonly results: HomeResultsService,
    @inject(ResultsMaximize) private readonly maximize: ResultsMaximize,
  ) {
    super();
  }

  mount(_root: ParentNode = this.doc): void {
    if (!this.isHomeMounted()) return;
    // Idempotent: a second mount (e.g. bootstrap belt-and-suspenders after page
    // mount, or accidental double start) must not stack body click listeners.
    if (this.mounted) {
      this.maximize.mount();
      this.updateHomeActionButtons();
      return;
    }
    this.mounted = true;

    this.maximize.mount();
    this.wireResize();
    this.wireClearButton();
    this.wireProcedureGuards();
    this.wireHtmxSwaps();
    this.wireHtmxRequestGuards();
    this.updateHomeActionButtons();
  }

  unmount(): void {
    this.maximize.dispose();
    while (this.disposers.length) {
      this.disposers.pop()?.();
    }
    this.mounted = false;
  }

  // --- public for tests ---

  hasSelectedProcedure(): boolean {
    const input = this.getProcedureIdInput();
    return isValidProcedureId(input?.value);
  }

  hasExportableResults(): boolean {
    const root =
      this.doc.querySelector("#results-panel .js-results-root") ||
      this.doc.querySelector("#results-panel [data-export-ready]");
    return root?.getAttribute("data-export-ready") === "true";
  }

  updateHomeActionButtons(): void {
    if (!this.isHomeMounted()) return;

    const hasProcedure = this.hasSelectedProcedure();

    for (const id of ["btn-execute", "btn-clear"]) {
      const btn = this.doc.getElementById(id);
      if (!btn) continue;
      if ("disabled" in btn) {
        (btn as HTMLButtonElement).disabled = !hasProcedure;
      }
      btn.setAttribute("aria-disabled", hasProcedure ? "false" : "true");
    }

    this.setExportEnabled(canExport(hasProcedure, this.hasExportableResults()));

    const hint = this.doc.getElementById("procedure-selection-hint");
    if (hint) {
      hint.classList.toggle("hidden", hasProcedure);
    }
  }

  validateRequiredParameters(): string[] {
    this.clearRequiredParameterErrors();
    const missing: string[] = [];

    this.doc
      .querySelectorAll<HTMLElement>("#execution-parameters .js-param-field[data-required='true']")
      .forEach((field) => {
        const input = field.querySelector<
          HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement
        >(".js-param-input");
        if (!input) return;

        const value = (input.value || "").trim();
        if (!value) {
          const caption = field.getAttribute("data-caption") || input.name || "Parameter";
          missing.push(caption);
          input.classList.add("input-validation-error");
          const err = field.querySelector(".js-param-error");
          if (err) {
            const page = this.doc.querySelector("[data-msg-required-params]");
            const template =
              page?.getAttribute("data-msg-required-params") || "Required parameter: {0}";
            err.textContent = template.replace("{0}", caption);
            err.classList.remove("hidden");
          }
        }
      });

    return missing;
  }

  // --- private ---

  private isHomeMounted(): boolean {
    return !!(this.doc.getElementById("exec-form") || this.doc.querySelector(".js-procedure-item"));
  }

  private getProcedureIdInput(): HTMLInputElement | HTMLSelectElement | null {
    return (
      (this.doc.getElementById("procedureId") as HTMLInputElement | null) ||
      this.doc.querySelector<HTMLInputElement | HTMLSelectElement>(
        "#exec-form .js-procedure-select",
      ) ||
      this.doc.querySelector<HTMLInputElement | HTMLSelectElement>(".js-procedure-select")
    );
  }

  private setExportEnabled(enabled: boolean): void {
    const btn = this.doc.getElementById("btn-export");
    if (!btn) return;
    if ("disabled" in btn) {
      (btn as HTMLButtonElement).disabled = !enabled;
    }
    btn.setAttribute("aria-disabled", enabled ? "false" : "true");
    const page = this.doc.querySelector("[data-msg-export-requires-data]");
    const requiresData =
      page?.getAttribute("data-msg-export-requires-data") ||
      "Execute a procedure that returns data before exporting.";
    const selectMsg =
      page?.getAttribute("data-msg-select-procedure") || "Select a procedure before executing.";
    btn.title = enabled ? requiresData : this.hasSelectedProcedure() ? requiresData : selectMsg;
  }

  private clearRequiredParameterErrors(): void {
    this.doc.querySelectorAll(".js-param-error").forEach((el) => {
      el.classList.add("hidden");
      el.textContent = "";
    });
    this.doc.querySelectorAll(".js-param-input.input-validation-error").forEach((el) => {
      el.classList.remove("input-validation-error");
    });
  }

  private selectProcedureItem(item: Element): void {
    const id = item.getAttribute("data-procedure-id") || "";
    const input = this.getProcedureIdInput();
    if (input) {
      input.value = id;
    }

    this.doc.querySelectorAll(".js-procedure-item").forEach((el) => {
      const selected = el === item;
      el.classList.toggle("is-selected", selected);
      el.setAttribute("aria-selected", selected ? "true" : "false");
    });

    this.setPageNumber(1);
    this.results.clearExportableResults();
    this.setExportEnabled(false);
    this.updateHomeActionButtons();
  }

  private setPageNumber(page: number): void {
    const input =
      (this.doc.getElementById("pageNumber") as HTMLInputElement | null) ||
      this.doc.querySelector<HTMLInputElement>("#exec-form .js-page-number");
    if (input) {
      input.value = String(page > 0 ? page : 1);
    }
  }

  private showResultsMessage(msg: string): void {
    const panel = this.doc.getElementById("results-panel");
    if (!panel) return;
    panel.innerHTML =
      '<div class="js-results-root" data-export-ready="false" data-row-count="0"><div class="rounded border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-900"></div></div>';
    const box = panel.querySelector(".rounded");
    if (box) box.textContent = msg;
  }

  private wireResize(): void {
    const onResize = () => this.results.refreshLayout();
    this.win.addEventListener("resize", onResize);
    this.disposers.push(() => this.win.removeEventListener("resize", onResize));
  }

  private wireClearButton(): void {
    const onClick = (e: Event) => {
      const target = e.target as Element | null;
      const clear = target?.closest?.("#btn-clear");
      if (!clear || (clear as HTMLButtonElement).disabled) return;

      if (!this.hasSelectedProcedure()) {
        e.preventDefault();
        this.updateHomeActionButtons();
        return;
      }

      const url = clear.getAttribute("data-clear-url") || "/";
      this.win.location.href = url;
    };
    this.doc.body.addEventListener("click", onClick);
    this.disposers.push(() => this.doc.body.removeEventListener("click", onClick));
  }

  private wireProcedureGuards(): void {
    const form = this.doc.getElementById("exec-form");
    if (!form && !this.doc.querySelector(".js-procedure-item")) return;

    const onListClick = (e: Event) => {
      const item = (e.target as Element | null)?.closest?.(".js-procedure-item");
      if (!item) return;
      this.selectProcedureItem(item);
    };
    this.doc.body.addEventListener("click", onListClick);
    this.disposers.push(() => this.doc.body.removeEventListener("click", onListClick));

    if (form) {
      const onSubmit = (e: Event) => {
        e.preventDefault();
        e.stopPropagation();
        const executeBtn = this.doc.getElementById("btn-execute") as HTMLButtonElement | null;
        if (executeBtn && !executeBtn.disabled && this.hasSelectedProcedure()) {
          executeBtn.click();
        }
      };
      form.addEventListener("submit", onSubmit);
      this.disposers.push(() => form.removeEventListener("submit", onSubmit));

      const invalidateExport = (e: Event) => {
        const t = e.target as HTMLInputElement | null;
        if (!t || t.id === "procedureId" || t.name === "procedureId") return;
        if (isParamFieldName(t.name)) {
          this.setExportEnabled(false);
          const root = this.doc.querySelector("#results-panel .js-results-root");
          if (root) root.setAttribute("data-export-ready", "false");
        }
      };
      form.addEventListener("input", invalidateExport);
      form.addEventListener("change", invalidateExport);
      this.disposers.push(() => {
        form.removeEventListener("input", invalidateExport);
        form.removeEventListener("change", invalidateExport);
      });
    }
  }

  private wireHtmxSwaps(): void {
    const offAfter = this.htmx.onAfterSwap((event) => {
      const detail = (event as CustomEvent).detail as {
        target?: HTMLElement;
      };
      if (detail?.target?.id !== "results-panel") return;
      const root = detail.target.querySelector(".js-results-root");
      if (root) {
        this.results.initFromResultsRoot(root);
        // Keep form paging fields in sync with the rendered page (for export/re-execute).
        const pageAttr = root.getAttribute("data-page-number");
        const sizeAttr = root.getAttribute("data-page-size");
        if (pageAttr) {
          this.setPageNumber(Number.parseInt(pageAttr, 10) || 1);
        }
        if (sizeAttr) {
          const sizeInput =
            (this.doc.getElementById("pageSize") as HTMLInputElement | null) ||
            this.doc.querySelector<HTMLInputElement>("#exec-form .js-page-size");
          if (sizeInput) {
            sizeInput.value = sizeAttr;
          }
        }
      } else {
        this.results.destroyInResultsPanel();
      }
      this.updateHomeActionButtons();
    });
    this.disposers.push(offAfter);

    const offBefore = this.htmx.onBeforeSwap((event) => {
      const detail = (event as CustomEvent).detail as {
        target?: HTMLElement;
      };
      if (detail?.target?.id === "results-panel") {
        this.results.destroyInResultsPanel();
      }
    });
    this.disposers.push(offBefore);
  }

  private wireHtmxRequestGuards(): void {
    // Need full event (elt + preventDefault); HtmxBridge CSRF only gets headers.
    const onConfig = (event: Event) => {
      const custom = event as CustomEvent<{
        elt?: Element;
        headers: Record<string, string>;
        // HTMX 2 formData proxy — assignment mutates the outbound request body.
        parameters?: Record<string, unknown> & {
          delete?: (name: string) => void;
        };
      }>;
      const rawElt = custom.detail?.elt as HTMLElement | undefined;
      if (!rawElt) return;

      // Click target may be a child (icon/span); resolve to the HTMX host when needed.
      const pagerBtn = rawElt.classList?.contains("js-results-page")
        ? rawElt
        : (rawElt.closest?.(".js-results-page") as HTMLElement | null);
      const elt =
        pagerBtn ??
        (rawElt.id
          ? rawElt
          : ((rawElt.closest?.("#btn-execute, #btn-export") as HTMLElement | null) ?? rawElt));

      if (elt.id === "btn-execute") {
        if (!this.hasSelectedProcedure()) {
          custom.preventDefault();
          this.updateHomeActionButtons();
          const root = this.doc.querySelector("[data-msg-select-procedure]");
          const msg =
            root?.getAttribute("data-msg-select-procedure") ||
            "Select a procedure before executing.";
          this.showResultsMessage(msg);
          this.setExportEnabled(false);
          return;
        }

        const missing = this.validateRequiredParameters();
        if (missing.length > 0) {
          custom.preventDefault();
          this.setExportEnabled(false);
          const page = this.doc.querySelector("[data-msg-required-params]");
          const single =
            page?.getAttribute("data-msg-required-params") || "Fill required parameters: {0}";
          const multi =
            page?.getAttribute("data-msg-required-params-multi") || "Fill required parameters: {0}";
          this.showResultsMessage(formatRequiredParamsMessage(missing, single, multi));
          return;
        }

        // Fresh Execute always starts at page 1 (pager buttons set their own page).
        this.applyPageNumberToRequest(1, custom.detail?.parameters);
        return;
      }

      // Server-side results pager: form values were already read; override request params.
      if (pagerBtn) {
        const pageAttr = pagerBtn.getAttribute("data-page");
        const page = pageAttr ? Number.parseInt(pageAttr, 10) : 1;
        const safePage = Number.isFinite(page) && page > 0 ? page : 1;
        this.applyPageNumberToRequest(safePage, custom.detail?.parameters);
        return;
      }

      if (elt.id === "btn-export") {
        if (!this.hasSelectedProcedure() || !this.hasExportableResults()) {
          custom.preventDefault();
          this.updateHomeActionButtons();
          const status = this.doc.getElementById("export-status");
          if (status) {
            const page = this.doc.querySelector("[data-msg-export-requires-data]");
            const msg =
              page?.getAttribute("data-msg-export-requires-data") ||
              "Execute a procedure that returns data before exporting.";
            status.innerHTML = '<span class="text-sm text-red-700"></span>';
            status.firstElementChild!.textContent = msg;
          }
        }
      }
    };

    this.doc.body.addEventListener("htmx:configRequest", onConfig);
    this.disposers.push(() => this.doc.body.removeEventListener("htmx:configRequest", onConfig));
  }

  /**
   * Keep the hidden form field and the in-flight HTMX parameters in sync.
   * Updating only the DOM is too late: HTMX already collected form values.
   */
  private applyPageNumberToRequest(
    page: number,
    parameters?: Record<string, unknown> | null,
  ): void {
    const safe = page > 0 ? page : 1;
    this.setPageNumber(safe);
    if (parameters && typeof parameters === "object") {
      parameters.pageNumber = String(safe);
    }
  }
}
