import { COL_MAX, COL_MIN } from "./constants";
import { qs } from "./dom";
import { autoSizeColumns } from "./measure";
import { applyColumnWidths, buildAllRowHtml, renderHeaderCells, syncHeaderScroll } from "./render";
import { applyColumnReorder, compareSortValues } from "./sort";
import type { SheetColumn, SheetGridState, SheetPayload, SheetPayloadColumn } from "./types";

function parseColumns(raw: SheetPayloadColumn[] | undefined): SheetColumn[] {
    return (Array.isArray(raw) ? raw : []).map((c) => ({
        caption: c.caption || "",
        align: c.align || "left",
        html: !!c.html,
        sortable: c.sortable !== false,
        width: typeof c.width === "number" ? c.width : COL_MIN,
        fixedWidth: typeof c.width === "number",
    }));
}

/**
 * One mounted sheet-grid instance (Clusterize + header interactions).
 */
export class SheetGrid {
    private state: SheetGridState | null = null;

    constructor(
        private readonly root: HTMLElement,
        private readonly win: Window = window,
    ) {}

    getState(): SheetGridState | null {
        return this.state;
    }

    mount(): SheetGridState | null {
        const ClusterizeCtor =
            this.win.Clusterize ?? (typeof Clusterize !== "undefined" ? Clusterize : undefined);
        if (!ClusterizeCtor) return null;

        this.destroy();

        const scroll = qs(this.root, ".js-sheet-scroll", ".js-results-scroll");
        const content = qs(this.root, ".js-sheet-content", ".js-results-content");
        const dataEl = qs(this.root, ".js-sheet-data", ".js-results-data");
        if (!scroll || !content || !dataEl) return null;

        let payload: SheetPayload = { columns: [], cells: [] };
        try {
            payload = JSON.parse(dataEl.textContent || "{}") as SheetPayload;
        } catch {
            payload = { columns: [], cells: [] };
        }

        const columns = parseColumns(payload.columns);
        const cells = Array.isArray(payload.cells)
            ? payload.cells.map((row) => (Array.isArray(row) ? [...row] : []))
            : [];
        if (!columns.length) return null;

        autoSizeColumns(columns, cells, this.root, this.win);

        const state: SheetGridState = {
            root: this.root,
            columns,
            cells,
            sortCol: null,
            sortAsc: true,
            suppressClick: false,
            clusterize: null,
            onScroll: null,
        };
        this.state = state;

        this.renderHeaders();
        const rows = buildAllRowHtml(state);

        state.clusterize = new ClusterizeCtor({
            rows,
            scrollElem: scroll,
            contentElem: content,
            tag: "tr",
            rows_in_block: 50,
            blocks_in_cluster: 4,
            callbacks: {
                clusterChanged: () => {
                    syncHeaderScroll(this.root);
                },
            },
        });

        const settle = () => {
            if (!this.state) return;
            applyColumnWidths(this.state);
            syncHeaderScroll(this.root);
            try {
                this.state.clusterize?.refresh(true);
            } catch {
                // ignore
            }
        };
        this.win.requestAnimationFrame(() => this.win.requestAnimationFrame(settle));
        this.win.setTimeout(settle, 50);

        state.onScroll = () => syncHeaderScroll(this.root);
        scroll.addEventListener("scroll", state.onScroll, { passive: true });

        return state;
    }

    destroy(): void {
        const state = this.state;
        if (!state) return;
        if (state.clusterize) {
            try {
                state.clusterize.destroy(true);
            } catch {
                // ignore
            }
        }
        if (state.onScroll) {
            const scroll = qs(this.root, ".js-sheet-scroll", ".js-results-scroll");
            scroll?.removeEventListener("scroll", state.onScroll);
        }
        this.state = null;
    }

    refresh(): void {
        const state = this.state;
        if (!state) return;
        applyColumnWidths(state);
        syncHeaderScroll(this.root);
        try {
            state.clusterize?.refresh(true);
        } catch {
            // ignore
        }
    }

    private refreshClusterize(): void {
        const state = this.state;
        if (!state?.clusterize) return;
        state.clusterize.update(buildAllRowHtml(state));
        applyColumnWidths(state);
        syncHeaderScroll(this.root);
        try {
            state.clusterize.refresh(true);
        } catch {
            // ignore
        }
    }

    private sortByColumn(colIndex: number): void {
        const state = this.state;
        if (!state || state.columns[colIndex]?.sortable === false) return;

        const asc = state.sortCol === colIndex ? !state.sortAsc : true;
        state.sortCol = colIndex;
        state.sortAsc = asc;

        const indices = state.cells.map((_, i) => i);
        indices.sort((ia, ib) =>
            compareSortValues(state.cells[ia]?.[colIndex], state.cells[ib]?.[colIndex], asc),
        );
        state.cells = indices.map((i) => state.cells[i]);

        this.renderHeaders();
        this.refreshClusterize();
    }

    private reorderColumn(fromIndex: number, toIndex: number): void {
        const state = this.state;
        if (!state) return;
        state.sortCol = applyColumnReorder(
            state.columns,
            state.cells,
            fromIndex,
            toIndex,
            state.sortCol,
        );
        this.renderHeaders();
        this.refreshClusterize();
    }

    private renderHeaders(): void {
        const state = this.state;
        if (!state) return;
        const row = qs(state.root, ".js-sheet-header-row", ".js-results-header-row");
        if (!row) return;

        row.innerHTML = renderHeaderCells(state);
        applyColumnWidths(state);
        this.wireHeaderInteractions(row);
    }

    private wireHeaderInteractions(row: HTMLElement): void {
        const state = this.state;
        if (!state) return;

        row.querySelectorAll(".qp-sheet-th").forEach((thEl) => {
            const th = thEl as HTMLElement;
            th.addEventListener("click", (e) => {
                if ((e.target as Element | null)?.closest?.(".qp-sheet-col-resizer")) {
                    return;
                }
                if (state.suppressClick) {
                    state.suppressClick = false;
                    return;
                }
                const colIndex = Number(th.dataset.colIndex);
                if (Number.isNaN(colIndex)) return;
                this.sortByColumn(colIndex);
            });
        });

        row.querySelectorAll(".qp-sheet-col-resizer").forEach((handleEl) => {
            const handle = handleEl as HTMLElement;
            handle.addEventListener("mousedown", (e) => {
                e.preventDefault();
                e.stopPropagation();
                const colIndex = Number(handle.dataset.resizeCol);
                if (Number.isNaN(colIndex) || !state.columns[colIndex]) return;

                const startX = e.clientX;
                const startW = state.columns[colIndex].width;
                state.suppressClick = true;
                this.win.document.body.classList.add("qp-col-resizing");

                const onMove = (ev: MouseEvent) => {
                    state.columns[colIndex].width = Math.round(
                        Math.min(COL_MAX, Math.max(COL_MIN, startW + (ev.clientX - startX))),
                    );
                    applyColumnWidths(state);
                };
                const onUp = () => {
                    this.win.document.removeEventListener("mousemove", onMove);
                    this.win.document.removeEventListener("mouseup", onUp);
                    this.win.document.body.classList.remove("qp-col-resizing");
                    this.win.setTimeout(() => {
                        state.suppressClick = false;
                    }, 0);
                    try {
                        state.clusterize?.refresh(true);
                    } catch {
                        // ignore
                    }
                };
                this.win.document.addEventListener("mousemove", onMove);
                this.win.document.addEventListener("mouseup", onUp);
            });
        });

        let dragFrom: number | null = null;
        row.querySelectorAll(".qp-sheet-th").forEach((thEl) => {
            const th = thEl as HTMLElement;
            th.addEventListener("dragstart", (e) => {
                if ((e.target as Element | null)?.closest?.(".qp-sheet-col-resizer")) {
                    e.preventDefault();
                    return;
                }
                dragFrom = Number(th.dataset.colIndex);
                state.suppressClick = true;
                th.classList.add("is-dragging");
                if (e.dataTransfer) {
                    e.dataTransfer.effectAllowed = "move";
                    try {
                        e.dataTransfer.setData("text/plain", String(dragFrom));
                    } catch {
                        // ignore
                    }
                }
            });
            th.addEventListener("dragend", () => {
                th.classList.remove("is-dragging");
                row.querySelectorAll(".qp-sheet-th").forEach((el) =>
                    el.classList.remove("is-drop-target"),
                );
                dragFrom = null;
                this.win.setTimeout(() => {
                    state.suppressClick = false;
                }, 0);
            });
            th.addEventListener("dragover", (e) => {
                e.preventDefault();
                if (e.dataTransfer) e.dataTransfer.dropEffect = "move";
                th.classList.add("is-drop-target");
            });
            th.addEventListener("dragleave", () => {
                th.classList.remove("is-drop-target");
            });
            th.addEventListener("drop", (e) => {
                e.preventDefault();
                th.classList.remove("is-drop-target");
                const toIndex = Number(th.dataset.colIndex);
                const fromIndex =
                    dragFrom !== null ? dragFrom : Number(e.dataTransfer?.getData("text/plain"));
                if (Number.isNaN(fromIndex) || Number.isNaN(toIndex)) return;
                this.reorderColumn(fromIndex, toIndex);
            });
        });
    }
}
