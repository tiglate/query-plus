import { COL_MIN } from "./constants";
import { escapeHtml, qs } from "./dom";
import { totalColumnsWidth } from "./measure";
import type { SheetColumn, SheetGridState } from "./types";

export function buildRowHtml(rowCells: string[], columns: SheetColumn[]): string {
    let html = "<tr>";
    for (let c = 0; c < columns.length; c++) {
        const align = columns[c].align || "left";
        const raw = rowCells[c] ?? "";
        const body = columns[c].html ? String(raw) : escapeHtml(raw);
        html += `<td class="qp-sheet-cell" style="text-align:${align}">${body}</td>`;
    }
    html += "</tr>";
    return html;
}

export function buildAllRowHtml(state: SheetGridState): string[] {
    return state.cells.map((row) => buildRowHtml(row, state.columns));
}

export function applyColumnWidths(state: SheetGridState): void {
    const root = state.root;
    const total = totalColumnsWidth(state.columns);
    const headerTable = qs(root, ".js-sheet-headers-table", ".js-results-headers-table");
    const bodyTable = qs(root, ".js-sheet-body-table", ".js-results-body-table");
    const headerGroup = qs(root, ".js-sheet-colgroup-header", ".js-results-colgroup-header");
    const bodyGroup = qs(root, ".js-sheet-colgroup-body", ".js-results-colgroup-body");
    if (!headerTable || !bodyTable || !headerGroup || !bodyGroup) return;

    const colsHtml = state.columns
        .map((col) => `<col style="width:${col.width || COL_MIN}px">`)
        .join("");
    headerGroup.innerHTML = colsHtml;
    bodyGroup.innerHTML = colsHtml;

    headerTable.style.width = total + "px";
    bodyTable.style.width = total + "px";
    headerTable.style.minWidth = total + "px";
    bodyTable.style.minWidth = total + "px";
}

export function syncHeaderScroll(root: ParentNode): void {
    const scroll = qs(root, ".js-sheet-scroll", ".js-results-scroll");
    const headers = qs(root, ".qp-sheet-headers", ".qp-results-headers");
    if (!scroll || !headers) return;
    const headerTable = headers.querySelector("table");
    if (!(headerTable instanceof HTMLElement)) return;
    headerTable.style.marginLeft = -scroll.scrollLeft + "px";
}

export function renderHeaderCells(state: SheetGridState): string {
    const parts: string[] = [];
    for (let c = 0; c < state.columns.length; c++) {
        const col = state.columns[c];
        const sortable = col.sortable !== false;
        const sorted = state.sortCol === c;
        const sortClass = sorted ? (state.sortAsc ? "fa-sort-up" : "fa-sort-down") : "fa-sort";
        const dirAttr = sorted ? ` data-sort-dir="${state.sortAsc ? "asc" : "desc"}"` : "";
        const dragAttr = sortable ? ` draggable="true"` : "";
        const title = sortable
            ? "Click to sort · Drag to reorder · Drag edge to resize"
            : "Drag edge to resize";

        parts.push(
            `<th class="qp-sheet-th" data-col-index="${c}" data-sort-col="${c}"` +
                ` style="text-align:${col.align || "left"}" title="${title}"${dirAttr}${dragAttr}>` +
                `<span class="qp-sheet-th-label">${escapeHtml(col.caption)}</span>` +
                (sortable
                    ? `<i class="fa-solid ${sortClass} qp-sheet-th-sort" aria-hidden="true"></i>`
                    : "") +
                `<span class="qp-sheet-col-resizer" data-resize-col="${c}" title="Resize column"></span>` +
                `</th>`,
        );
    }
    return parts.join("");
}
