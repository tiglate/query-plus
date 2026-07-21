import { inject, injectable, singleton } from "tsyringe";
import { TOKENS } from "@/core/di/tokens";
import { HomeResultsService } from "./HomeResultsService";

export const RESULTS_MAX_STORAGE_KEY = "qp-home-results-maximized";

@singleton()
@injectable()
export class ResultsMaximize {
    private unsub: (() => void) | null = null;

    constructor(
        @inject(TOKENS.Document) private readonly doc: Document,
        @inject(TOKENS.Window) private readonly win: Window,
        @inject(HomeResultsService) private readonly results: HomeResultsService,
    ) {}

    isMaximized(): boolean {
        return !!this.doc.querySelector(".qp-home-columns.is-results-maximized");
    }

    setMaximized(maximized: boolean): void {
        const columns = this.doc.querySelector(".qp-home-columns");
        const btn = this.doc.getElementById("btn-toggle-results-max");
        if (!columns) return;

        columns.classList.toggle("is-results-maximized", maximized);

        if (btn) {
            const labelMaximize = btn.getAttribute("data-label-maximize") || "Maximize";
            const labelRestore = btn.getAttribute("data-label-restore") || "Restore";
            const label = maximized ? labelRestore : labelMaximize;
            const icon = btn.querySelector(".js-results-max-icon");
            const text = btn.querySelector(".js-results-max-label");

            btn.setAttribute("aria-pressed", maximized ? "true" : "false");
            btn.title = label;
            if (text) text.textContent = label;
            if (icon) {
                icon.classList.toggle("fa-expand", !maximized);
                icon.classList.toggle("fa-compress", maximized);
            }
        }

        try {
            this.win.sessionStorage.setItem(RESULTS_MAX_STORAGE_KEY, maximized ? "1" : "0");
        } catch {
            // private mode / blocked storage
        }
    }

    mount(): void {
        this.dispose();

        const columns = this.doc.querySelector(".qp-home-columns");
        const btn = this.doc.getElementById("btn-toggle-results-max");
        // Nothing to do if home results chrome is not on the page.
        if (!columns || !btn) return;

        let initial = false;
        try {
            initial = this.win.sessionStorage.getItem(RESULTS_MAX_STORAGE_KEY) === "1";
        } catch {
            initial = false;
        }
        this.setMaximized(initial);

        // Delegate from body so we still handle clicks on icon/label children.
        const onClick = (e: Event) => {
            const target = e.target;
            if (!(target instanceof Element)) return;
            const clicked = target.closest("#btn-toggle-results-max");
            if (!clicked || !this.doc.contains(clicked)) return;

            e.preventDefault();
            e.stopPropagation();
            this.setMaximized(!this.isMaximized());
            this.win.requestAnimationFrame(() => {
                this.results.refreshLayout();
            });
        };

        this.doc.body.addEventListener("click", onClick);
        this.unsub = () => this.doc.body.removeEventListener("click", onClick);
    }

    dispose(): void {
        this.unsub?.();
        this.unsub = null;
    }
}
