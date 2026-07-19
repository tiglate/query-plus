/**
 * Shared Excel-like sheet grid (Clusterize.js).
 * Used by home execution results and admin CRUD lists.
 *
 * Markup (inside root):
 *   .js-sheet-headers-table / .js-sheet-header-row / .js-sheet-colgroup-header
 *   .js-sheet-scroll / .js-sheet-body-table / .js-sheet-content / .js-sheet-colgroup-body
 *   script.js-sheet-data  →  { columns: [{caption, align, html?, sortable?, width?}], cells: string[][] }
 *
 * Also accepts legacy home aliases: .js-results-*
 */
(function (window) {
  "use strict";

  const COL_MIN = 48;
  const COL_MAX = 480;
  const COL_PAD = 28;
  const WIDTH_SAMPLE = 200;

  /** @type {WeakMap<Element, object>} */
  const instances = new WeakMap();
  let measureCanvasCtx = null;

  function qs(root, ...selectors) {
    for (const sel of selectors) {
      const el = root.querySelector(sel);
      if (el) return el;
    }
    return null;
  }

  function escapeHtml(text) {
    return String(text ?? "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#39;");
  }

  function getMeasureContext(root) {
    if (!measureCanvasCtx) {
      const canvas = document.createElement("canvas");
      measureCanvasCtx = canvas.getContext("2d");
    }
    const sample =
      root?.querySelector(".qp-sheet-th") ||
      root?.querySelector(".qp-sheet") ||
      document.body;
    const style = window.getComputedStyle(sample);
    measureCanvasCtx.font = `${style.fontWeight || "600"} ${style.fontSize || "12px"} ${
      style.fontFamily || "Inter, Segoe UI, system-ui, sans-serif"
    }`;
    return measureCanvasCtx;
  }

  function measureTextWidth(ctx, text) {
    if (!text) return 0;
    const sample = text.length > 80 ? text.slice(0, 80) + "…" : text;
    // Strip simple HTML for sizing action columns roughly.
    const plain = sample.replace(/<[^>]+>/g, " ");
    return ctx.measureText(plain).width;
  }

  function autoSizeColumns(columns, cells, root) {
    const ctx = getMeasureContext(root);
    const sample = Math.min(WIDTH_SAMPLE, cells.length);

    for (let c = 0; c < columns.length; c++) {
      if (columns[c].width && columns[c].fixedWidth) continue;

      let maxPx = measureTextWidth(ctx, columns[c].caption || "");
      if (columns[c].html) {
        // Prefer explicit width for action/html columns.
        maxPx = Math.max(maxPx, columns[c].width || 180);
      } else {
        for (let r = 0; r < sample; r++) {
          const w = measureTextWidth(ctx, cells[r]?.[c] || "");
          if (w > maxPx) maxPx = w;
        }
      }
      columns[c].width = Math.round(
        Math.min(COL_MAX, Math.max(COL_MIN, maxPx + COL_PAD))
      );
    }
  }

  function totalColumnsWidth(columns) {
    return columns.reduce((sum, col) => sum + (col.width || COL_MIN), 0);
  }

  function buildRowHtml(rowCells, columns) {
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

  function buildAllRowHtml(state) {
    return state.cells.map((row) => buildRowHtml(row, state.columns));
  }

  function applyColumnWidths(state) {
    const root = state.root;
    const total = totalColumnsWidth(state.columns);
    const headerTable = qs(root, ".js-sheet-headers-table", ".js-results-headers-table");
    const bodyTable = qs(root, ".js-sheet-body-table", ".js-results-body-table");
    const headerGroup = qs(root, ".js-sheet-colgroup-header", ".js-results-colgroup-header");
    const bodyGroup = qs(root, ".js-sheet-colgroup-body", ".js-results-colgroup-body");
    if (!headerTable || !bodyTable || !headerGroup || !bodyGroup) return;

    const colsHtml = state.columns
      .map((col) => `<col style="width:${col.width}px">`)
      .join("");
    headerGroup.innerHTML = colsHtml;
    bodyGroup.innerHTML = colsHtml;

    headerTable.style.width = total + "px";
    bodyTable.style.width = total + "px";
    headerTable.style.minWidth = total + "px";
    bodyTable.style.minWidth = total + "px";
  }

  function syncHeaderScroll(root) {
    const scroll = qs(root, ".js-sheet-scroll", ".js-results-scroll");
    const headers = qs(root, ".qp-sheet-headers", ".qp-results-headers");
    if (!scroll || !headers) return;
    const headerTable = headers.querySelector("table");
    if (!headerTable) return;
    headerTable.style.marginLeft = -scroll.scrollLeft + "px";
  }

  function compareSortValues(av, bv, asc) {
    const a = String(av ?? "")
      .replace(/<[^>]+>/g, " ")
      .trim();
    const b = String(bv ?? "")
      .replace(/<[^>]+>/g, " ")
      .trim();
    const an = Number(a.replace(",", "."));
    const bn = Number(b.replace(",", "."));
    if (!Number.isNaN(an) && !Number.isNaN(bn) && a !== "" && b !== "") {
      return asc ? an - bn : bn - an;
    }
    return asc ? a.localeCompare(b) : b.localeCompare(a);
  }

  function refreshClusterize(state) {
    if (!state.clusterize) return;
    state.clusterize.update(buildAllRowHtml(state));
    applyColumnWidths(state);
    syncHeaderScroll(state.root);
    try {
      state.clusterize.refresh(true);
    } catch {
      // ignore
    }
  }

  function sortByColumn(state, colIndex) {
    if (state.columns[colIndex]?.sortable === false) return;

    const asc = state.sortCol === colIndex ? !state.sortAsc : true;
    state.sortCol = colIndex;
    state.sortAsc = asc;

    const indices = state.cells.map((_, i) => i);
    indices.sort((ia, ib) =>
      compareSortValues(state.cells[ia]?.[colIndex], state.cells[ib]?.[colIndex], asc)
    );
    state.cells = indices.map((i) => state.cells[i]);

    renderHeaders(state);
    refreshClusterize(state);
  }

  function reorderColumn(state, fromIndex, toIndex) {
    if (fromIndex === toIndex || fromIndex < 0 || toIndex < 0) return;
    if (fromIndex >= state.columns.length || toIndex >= state.columns.length) return;

    const [col] = state.columns.splice(fromIndex, 1);
    state.columns.splice(toIndex, 0, col);

    for (let r = 0; r < state.cells.length; r++) {
      const row = state.cells[r];
      const [val] = row.splice(fromIndex, 1);
      row.splice(toIndex, 0, val);
    }

    if (state.sortCol === fromIndex) {
      state.sortCol = toIndex;
    } else if (state.sortCol !== null) {
      if (fromIndex < state.sortCol && toIndex >= state.sortCol) state.sortCol -= 1;
      else if (fromIndex > state.sortCol && toIndex <= state.sortCol) state.sortCol += 1;
    }

    renderHeaders(state);
    refreshClusterize(state);
  }

  function wireHeaderInteractions(state) {
    const row = qs(state.root, ".js-sheet-header-row", ".js-results-header-row");
    if (!row) return;

    row.querySelectorAll(".qp-sheet-th").forEach((th) => {
      th.addEventListener("click", (e) => {
        if (e.target.closest(".qp-sheet-col-resizer")) return;
        if (state.suppressClick) {
          state.suppressClick = false;
          return;
        }
        const colIndex = Number(th.dataset.colIndex);
        if (Number.isNaN(colIndex)) return;
        sortByColumn(state, colIndex);
      });
    });

    row.querySelectorAll(".qp-sheet-col-resizer").forEach((handle) => {
      handle.addEventListener("mousedown", (e) => {
        e.preventDefault();
        e.stopPropagation();
        const colIndex = Number(handle.dataset.resizeCol);
        if (Number.isNaN(colIndex) || !state.columns[colIndex]) return;

        const startX = e.clientX;
        const startW = state.columns[colIndex].width;
        state.suppressClick = true;
        document.body.classList.add("qp-col-resizing");

        const onMove = (ev) => {
          state.columns[colIndex].width = Math.round(
            Math.min(COL_MAX, Math.max(COL_MIN, startW + (ev.clientX - startX)))
          );
          applyColumnWidths(state);
        };
        const onUp = () => {
          document.removeEventListener("mousemove", onMove);
          document.removeEventListener("mouseup", onUp);
          document.body.classList.remove("qp-col-resizing");
          setTimeout(() => {
            state.suppressClick = false;
          }, 0);
          try {
            state.clusterize?.refresh(true);
          } catch {
            // ignore
          }
        };
        document.addEventListener("mousemove", onMove);
        document.addEventListener("mouseup", onUp);
      });
    });

    let dragFrom = null;
    row.querySelectorAll(".qp-sheet-th").forEach((th) => {
      th.addEventListener("dragstart", (e) => {
        if (e.target.closest(".qp-sheet-col-resizer")) {
          e.preventDefault();
          return;
        }
        dragFrom = Number(th.dataset.colIndex);
        state.suppressClick = true;
        th.classList.add("is-dragging");
        e.dataTransfer.effectAllowed = "move";
        try {
          e.dataTransfer.setData("text/plain", String(dragFrom));
        } catch {
          // ignore
        }
      });
      th.addEventListener("dragend", () => {
        th.classList.remove("is-dragging");
        row
          .querySelectorAll(".qp-sheet-th")
          .forEach((el) => el.classList.remove("is-drop-target"));
        dragFrom = null;
        setTimeout(() => {
          state.suppressClick = false;
        }, 0);
      });
      th.addEventListener("dragover", (e) => {
        e.preventDefault();
        e.dataTransfer.dropEffect = "move";
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
          dragFrom !== null
            ? dragFrom
            : Number(e.dataTransfer.getData("text/plain"));
        if (Number.isNaN(fromIndex) || Number.isNaN(toIndex)) return;
        reorderColumn(state, fromIndex, toIndex);
      });
    });
  }

  function renderHeaders(state) {
    const row = qs(state.root, ".js-sheet-header-row", ".js-results-header-row");
    if (!row) return;

    const parts = [];
    for (let c = 0; c < state.columns.length; c++) {
      const col = state.columns[c];
      const sortable = col.sortable !== false;
      const sorted = state.sortCol === c;
      const sortClass = sorted
        ? state.sortAsc
          ? "fa-sort-up"
          : "fa-sort-down"
        : "fa-sort";
      const dirAttr = sorted
        ? ` data-sort-dir="${state.sortAsc ? "asc" : "desc"}"`
        : "";
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
          `</th>`
      );
    }
    row.innerHTML = parts.join("");
    applyColumnWidths(state);
    wireHeaderInteractions(state);
  }

  function destroy(root) {
    const state = instances.get(root);
    if (!state) return;
    if (state.clusterize) {
      try {
        state.clusterize.destroy(true);
      } catch {
        // ignore
      }
    }
    if (state.onScroll) {
      const scroll = qs(root, ".js-sheet-scroll", ".js-results-scroll");
      scroll?.removeEventListener("scroll", state.onScroll);
    }
    instances.delete(root);
  }

  function mount(root) {
    if (!root || typeof Clusterize === "undefined") return null;
    destroy(root);

    const scroll = qs(root, ".js-sheet-scroll", ".js-results-scroll");
    const content = qs(root, ".js-sheet-content", ".js-results-content");
    const dataEl = qs(root, ".js-sheet-data", ".js-results-data");
    if (!scroll || !content || !dataEl) return null;

    let payload = { columns: [], cells: [] };
    try {
      payload = JSON.parse(dataEl.textContent || "{}");
    } catch {
      payload = { columns: [], cells: [] };
    }

    const columns = (Array.isArray(payload.columns) ? payload.columns : []).map(
      (c) => ({
        caption: c.caption || "",
        align: c.align || "left",
        html: !!c.html,
        sortable: c.sortable !== false,
        width: typeof c.width === "number" ? c.width : COL_MIN,
        fixedWidth: typeof c.width === "number",
      })
    );
    const cells = Array.isArray(payload.cells) ? payload.cells : [];
    if (!columns.length) return null;

    autoSizeColumns(columns, cells, root);

    const state = {
      root,
      columns,
      cells,
      sortCol: null,
      sortAsc: true,
      suppressClick: false,
      clusterize: null,
      onScroll: null,
    };

    renderHeaders(state);
    const rows = buildAllRowHtml(state);

    state.clusterize = new Clusterize({
      rows,
      scrollElem: scroll,
      contentElem: content,
      tag: "tr",
      rows_in_block: 50,
      blocks_in_cluster: 4,
      callbacks: {
        clusterChanged: function () {
          syncHeaderScroll(root);
        },
      },
    });

    const settle = () => {
      applyColumnWidths(state);
      syncHeaderScroll(root);
      try {
        state.clusterize?.refresh(true);
      } catch {
        // ignore
      }
    };
    requestAnimationFrame(() => requestAnimationFrame(settle));
    setTimeout(settle, 50);

    state.onScroll = () => syncHeaderScroll(root);
    scroll.addEventListener("scroll", state.onScroll, { passive: true });

    instances.set(root, state);
    return state;
  }

  function mountAll(selector) {
    document.querySelectorAll(selector).forEach((el) => mount(el));
  }

  function refresh(root) {
    const state = instances.get(root);
    if (!state) return;
    applyColumnWidths(state);
    syncHeaderScroll(root);
    try {
      state.clusterize?.refresh(true);
    } catch {
      // ignore
    }
  }

  window.QueryPlusSheetGrid = {
    mount,
    mountAll,
    destroy,
    refresh,
    getState(root) {
      return instances.get(root) || null;
    },
  };
})(window);
