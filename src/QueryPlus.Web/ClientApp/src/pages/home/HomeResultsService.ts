import { inject, injectable, singleton } from "tsyringe";
import { SheetGridService } from "../../components/sheet-grid/SheetGridService";
import { TOKENS } from "../../core/di/tokens";

/**
 * Home results panel: mount/destroy/refresh sheet grids after HTMX swaps.
 */
@singleton()
@injectable()
export class HomeResultsService {
  constructor(
    @inject(TOKENS.Document) private readonly doc: Document,
    @inject(SheetGridService) private readonly sheetGrid: SheetGridService,
  ) {}

  destroyInResultsPanel(): void {
    const panel = this.doc.getElementById("results-panel");
    if (!panel) return;
    panel.querySelectorAll(".js-sheet-root").forEach((el) => {
      this.sheetGrid.destroy(el);
    });
  }

  initFromResultsRoot(root: Element | null | undefined): void {
    if (!root) {
      this.destroyInResultsPanel();
      return;
    }
    const sheet =
      root instanceof HTMLElement && root.matches?.(".js-sheet-root")
        ? root
        : root.querySelector?.(".js-sheet-root");
    if (!sheet) {
      this.destroyInResultsPanel();
      return;
    }
    this.sheetGrid.mount(sheet);
  }

  refreshLayout(): void {
    const panel = this.doc.getElementById("results-panel");
    if (!panel) return;
    panel.querySelectorAll(".js-sheet-root").forEach((el) => {
      this.sheetGrid.refresh(el);
    });
  }

  clearExportableResults(): void {
    this.destroyInResultsPanel();
    const panel = this.doc.getElementById("results-panel");
    if (panel) {
      panel.innerHTML =
        '<div class="js-results-root" data-export-ready="false" data-row-count="0"><p class="qp-results-empty text-xs text-slate-500"></p></div>';
    }
    const status = this.doc.getElementById("export-status");
    if (status) status.innerHTML = "";
  }
}
