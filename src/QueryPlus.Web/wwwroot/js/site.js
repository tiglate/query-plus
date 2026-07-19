// Lightweight helpers for HTMX-driven UI (no SPA framework).

/**
 * Home results grid (Clusterize.js)
 * ---------------------------------
 * Structured client state: columns + cell matrix.
 * - Auto-width from header + sample of ~200 rows (canvas measureText)
 * - User resize via header edge drag
 * - User reorder via header drag-and-drop
 * - Sort via click (not drag)
 */
let resultsClusterize = null;
/** @type {null | {
 *   root: Element,
 *   columns: { caption: string, align: string, width: number }[],
 *   cells: string[][],
 *   sortCol: number | null,
 *   sortAsc: boolean,
 *   suppressClick: boolean
 * }} */
let resultsGridState = null;

const RESULTS_COL_MIN = 48;
const RESULTS_COL_MAX = 480;
const RESULTS_COL_PAD = 28; // cell padding + sort icon room
const RESULTS_WIDTH_SAMPLE = 200;

let measureCanvasCtx = null;

function destroyResultsClusterize() {
  if (resultsClusterize) {
    try {
      resultsClusterize.destroy(true);
    } catch {
      // instance may already be gone after HTMX swap
    }
    resultsClusterize = null;
  }
  resultsGridState = null;
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
  // Match rendered sheet font when possible.
  const sample =
    root?.querySelector(".qp-sheet-th") ||
    root?.querySelector(".qp-sheet") ||
    document.body;
  const style = window.getComputedStyle(sample);
  const weight = style.fontWeight || "600";
  const size = style.fontSize || "12px";
  const family = style.fontFamily || "Inter, Segoe UI, system-ui, sans-serif";
  measureCanvasCtx.font = `${weight} ${size} ${family}`;
  return measureCanvasCtx;
}

function measureTextWidth(ctx, text) {
  if (!text) return 0;
  // Cap super-long strings so a single outlier doesn't dominate.
  const sample = text.length > 80 ? text.slice(0, 80) + "…" : text;
  return ctx.measureText(sample).width;
}

/**
 * Auto column widths from header captions + a sample of data rows.
 * O(columns × sampleSize) — cheap even for large result sets.
 */
function autoSizeColumns(columns, cells, root) {
  const ctx = getMeasureContext(root);
  const sample = Math.min(RESULTS_WIDTH_SAMPLE, cells.length);

  for (let c = 0; c < columns.length; c++) {
    let maxPx = measureTextWidth(ctx, columns[c].caption || "");
    for (let r = 0; r < sample; r++) {
      const w = measureTextWidth(ctx, cells[r]?.[c] || "");
      if (w > maxPx) maxPx = w;
    }
    columns[c].width = Math.round(
      Math.min(RESULTS_COL_MAX, Math.max(RESULTS_COL_MIN, maxPx + RESULTS_COL_PAD))
    );
  }
}

function totalColumnsWidth(columns) {
  return columns.reduce((sum, col) => sum + (col.width || RESULTS_COL_MIN), 0);
}

function buildRowHtml(rowCells, columns) {
  let html = "<tr>";
  for (let c = 0; c < columns.length; c++) {
    const align = columns[c].align || "left";
    html +=
      `<td class="qp-sheet-cell" style="text-align:${align}">` +
      escapeHtml(rowCells[c] ?? "") +
      "</td>";
  }
  html += "</tr>";
  return html;
}

function buildAllRowHtml(state) {
  const rows = new Array(state.cells.length);
  for (let i = 0; i < state.cells.length; i++) {
    rows[i] = buildRowHtml(state.cells[i], state.columns);
  }
  return rows;
}

function applyColumnWidths(root, columns) {
  const total = totalColumnsWidth(columns);
  const headerTable = root.querySelector(".js-results-headers-table");
  const bodyTable = root.querySelector(".js-results-body-table");
  const headerGroup = root.querySelector(".js-results-colgroup-header");
  const bodyGroup = root.querySelector(".js-results-colgroup-body");
  if (!headerTable || !bodyTable || !headerGroup || !bodyGroup) return;

  const colsHtml = columns
    .map((col) => `<col style="width:${col.width}px">`)
    .join("");
  headerGroup.innerHTML = colsHtml;
  bodyGroup.innerHTML = colsHtml;

  headerTable.style.width = total + "px";
  bodyTable.style.width = total + "px";
  headerTable.style.minWidth = total + "px";
  bodyTable.style.minWidth = total + "px";
}

function renderResultsHeaders(state) {
  const row = state.root.querySelector(".js-results-header-row");
  if (!row) return;

  const parts = [];
  for (let c = 0; c < state.columns.length; c++) {
    const col = state.columns[c];
    const sorted = state.sortCol === c;
    const sortClass = sorted
      ? state.sortAsc
        ? "fa-sort-up"
        : "fa-sort-down"
      : "fa-sort";
    const dirAttr = sorted
      ? ` data-sort-dir="${state.sortAsc ? "asc" : "desc"}"`
      : "";
    parts.push(
      `<th class="qp-sheet-th" draggable="true" data-col-index="${c}" data-sort-col="${c}"` +
        ` style="text-align:${col.align || "left"}" title="Click to sort · Drag to reorder · Drag edge to resize"${dirAttr}>` +
        `<span class="qp-sheet-th-label">${escapeHtml(col.caption)}</span>` +
        `<i class="fa-solid ${sortClass} qp-sheet-th-sort" aria-hidden="true"></i>` +
        `<span class="qp-sheet-col-resizer" data-resize-col="${c}" title="Resize column"></span>` +
        `</th>`
    );
  }
  row.innerHTML = parts.join("");
  applyColumnWidths(state.root, state.columns);
  wireHeaderInteractions(state);
}

function syncResultsHeaderScroll(root) {
  const scroll = root?.querySelector(".js-results-scroll");
  const headers = root?.querySelector(".qp-results-headers");
  if (!scroll || !headers) return;
  const headerTable = headers.querySelector("table");
  if (!headerTable) return;
  headerTable.style.marginLeft = -scroll.scrollLeft + "px";
}

function refreshResultsClusterize() {
  if (!resultsGridState || !resultsClusterize) return;
  const rows = buildAllRowHtml(resultsGridState);
  resultsClusterize.update(rows);
  applyColumnWidths(resultsGridState.root, resultsGridState.columns);
  syncResultsHeaderScroll(resultsGridState.root);
  try {
    resultsClusterize.refresh(true);
  } catch {
    // ignore
  }
}

function compareSortValues(av, bv, asc) {
  const a = (av || "").trim();
  const b = (bv || "").trim();
  const an = Number(a.replace(",", "."));
  const bn = Number(b.replace(",", "."));
  if (!Number.isNaN(an) && !Number.isNaN(bn) && a !== "" && b !== "") {
    return asc ? an - bn : bn - an;
  }
  return asc ? a.localeCompare(b) : b.localeCompare(a);
}

function sortResultsGrid(colIndex) {
  if (!resultsGridState) return;
  const state = resultsGridState;
  const asc = state.sortCol === colIndex ? !state.sortAsc : true;
  state.sortCol = colIndex;
  state.sortAsc = asc;

  const indices = state.cells.map((_, i) => i);
  indices.sort((ia, ib) =>
    compareSortValues(state.cells[ia]?.[colIndex], state.cells[ib]?.[colIndex], asc)
  );
  state.cells = indices.map((i) => state.cells[i]);

  renderResultsHeaders(state);
  refreshResultsClusterize();
}

function reorderResultsColumn(fromIndex, toIndex) {
  if (!resultsGridState || fromIndex === toIndex) return;
  if (fromIndex < 0 || toIndex < 0) return;
  const state = resultsGridState;
  if (fromIndex >= state.columns.length || toIndex >= state.columns.length) return;

  const [col] = state.columns.splice(fromIndex, 1);
  state.columns.splice(toIndex, 0, col);

  for (let r = 0; r < state.cells.length; r++) {
    const row = state.cells[r];
    const [val] = row.splice(fromIndex, 1);
    row.splice(toIndex, 0, val);
  }

  // Sort column index follows the moved column when possible.
  if (state.sortCol === fromIndex) {
    state.sortCol = toIndex;
  } else if (state.sortCol !== null) {
    if (fromIndex < state.sortCol && toIndex >= state.sortCol) state.sortCol -= 1;
    else if (fromIndex > state.sortCol && toIndex <= state.sortCol) state.sortCol += 1;
  }

  renderResultsHeaders(state);
  refreshResultsClusterize();
}

function wireHeaderInteractions(state) {
  const row = state.root.querySelector(".js-results-header-row");
  if (!row || row.dataset.wired === "1") {
    // Always re-bind after innerHTML rebuild.
  }
  row.dataset.wired = "1";

  // --- Sort (click, not after drag/resize) ---
  row.querySelectorAll(".qp-sheet-th").forEach((th) => {
    th.addEventListener("click", (e) => {
      if (e.target.closest(".qp-sheet-col-resizer")) return;
      if (state.suppressClick) {
        state.suppressClick = false;
        return;
      }
      const colIndex = Number(th.dataset.colIndex);
      if (Number.isNaN(colIndex)) return;
      sortResultsGrid(colIndex);
    });
  });

  // --- Resize ---
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
        const delta = ev.clientX - startX;
        state.columns[colIndex].width = Math.round(
          Math.min(RESULTS_COL_MAX, Math.max(RESULTS_COL_MIN, startW + delta))
        );
        applyColumnWidths(state.root, state.columns);
      };
      const onUp = () => {
        document.removeEventListener("mousemove", onMove);
        document.removeEventListener("mouseup", onUp);
        document.body.classList.remove("qp-col-resizing");
        // Allow a short window so the click event after mouseup is suppressed.
        setTimeout(() => {
          state.suppressClick = false;
        }, 0);
        try {
          resultsClusterize?.refresh(true);
        } catch {
          // ignore
        }
      };
      document.addEventListener("mousemove", onMove);
      document.addEventListener("mouseup", onUp);
    });
  });

  // --- Reorder (HTML5 DnD) ---
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
      row.querySelectorAll(".qp-sheet-th").forEach((el) =>
        el.classList.remove("is-drop-target")
      );
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
      reorderResultsColumn(fromIndex, toIndex);
    });
  });
}

function initResultsClusterize(root) {
  destroyResultsClusterize();
  if (!root || typeof Clusterize === "undefined") return;

  const scroll = root.querySelector(".js-results-scroll");
  const content = root.querySelector(".js-results-content");
  const dataEl = root.querySelector(".js-results-data");
  if (!scroll || !content || !dataEl) return;

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
      width: RESULTS_COL_MIN,
    })
  );
  const cells = Array.isArray(payload.cells) ? payload.cells : [];
  if (!columns.length) return;

  // Auto-size once from a data sample (fast; ignores full million-row cost).
  autoSizeColumns(columns, cells, root);

  resultsGridState = {
    root,
    columns,
    cells,
    sortCol: null,
    sortAsc: true,
    suppressClick: false,
  };

  renderResultsHeaders(resultsGridState);
  const rows = buildAllRowHtml(resultsGridState);

  resultsClusterize = new Clusterize({
    rows,
    scrollElem: scroll,
    contentElem: content,
    tag: "tr",
    rows_in_block: 50,
    blocks_in_cluster: 4,
    callbacks: {
      clusterChanged: function () {
        syncResultsHeaderScroll(root);
      },
    },
  });

  const settleLayout = () => {
    applyColumnWidths(root, resultsGridState.columns);
    syncResultsHeaderScroll(root);
    try {
      resultsClusterize?.refresh(true);
    } catch {
      // ignore
    }
  };
  requestAnimationFrame(() => {
    requestAnimationFrame(settleLayout);
  });
  setTimeout(settleLayout, 50);

  scroll.addEventListener(
    "scroll",
    () => {
      syncResultsHeaderScroll(root);
    },
    { passive: true }
  );
}

document.addEventListener("DOMContentLoaded", () => {
  // Plain admin tables still support click-to-sort (non-Clusterize).
  document.body.addEventListener("click", (e) => {
    if (e.target.closest(".js-results-root")) return; // handled by sheet headers
    const th = e.target.closest("th[data-sort-col]");
    if (!th) return;
    const table = th.closest("table");
    const tbody = table?.querySelector("tbody");
    if (!tbody) return;

    const colIndex = Number(th.dataset.sortCol);
    const asc = th.dataset.sortDir !== "asc";
    th.dataset.sortDir = asc ? "asc" : "desc";
    th.parentElement?.querySelectorAll("th[data-sort-col]").forEach((other) => {
      if (other !== th) delete other.dataset.sortDir;
    });

    const bodyRows = Array.from(tbody.querySelectorAll("tr"));
    bodyRows.sort((a, b) => {
      const av = (a.children[colIndex]?.textContent || "").trim();
      const bv = (b.children[colIndex]?.textContent || "").trim();
      return compareSortValues(av, bv, asc);
    });
    bodyRows.forEach((r) => tbody.appendChild(r));
  });

  window.addEventListener("resize", () => {
    if (resultsGridState?.root) {
      applyColumnWidths(resultsGridState.root, resultsGridState.columns);
      syncResultsHeaderScroll(resultsGridState.root);
      try {
        resultsClusterize?.refresh(true);
      } catch {
        // ignore
      }
    }
  });
});

// Confirm delete forms
document.body.addEventListener("submit", (e) => {
  const form = e.target;
  if (form?.dataset?.confirm) {
    if (!window.confirm(form.dataset.confirm)) {
      e.preventDefault();
    }
  }
});

// Home screen action buttons — procedure id lives in a hidden input (list selection).
function getProcedureIdInput() {
  return (
    document.getElementById("procedureId") ||
    document.querySelector("#exec-form .js-procedure-select") ||
    document.querySelector(".js-procedure-select")
  );
}

function hasSelectedProcedure() {
  const select = getProcedureIdInput();
  return !!(select?.value || "").trim() && Number(select.value) > 0;
}

function selectProcedureItem(item) {
  if (!item) return;
  const id = item.getAttribute("data-procedure-id") || "";
  const input = getProcedureIdInput();
  if (input) {
    input.value = id;
  }

  document.querySelectorAll(".js-procedure-item").forEach((el) => {
    const selected = el === item;
    el.classList.toggle("is-selected", selected);
    el.setAttribute("aria-selected", selected ? "true" : "false");
  });

  clearExportableResults();
  updateHomeActionButtons();
}

function hasExportableResults() {
  const root =
    document.querySelector("#results-panel .js-results-root") ||
    document.querySelector("#results-panel [data-export-ready]");
  return root?.getAttribute("data-export-ready") === "true";
}

function setExportEnabled(enabled) {
  const btn = document.getElementById("btn-export");
  if (!btn) return;
  btn.disabled = !enabled;
  btn.setAttribute("aria-disabled", enabled ? "false" : "true");
  const page = document.querySelector("[data-msg-export-requires-data]");
  const requiresData =
    page?.getAttribute("data-msg-export-requires-data") ||
    "Execute a procedure that returns data before exporting.";
  const selectMsg =
    page?.getAttribute("data-msg-select-procedure") ||
    "Select a procedure before executing.";
  btn.title = enabled ? requiresData : hasSelectedProcedure() ? requiresData : selectMsg;
}

function updateHomeActionButtons() {
  // Home page may not be mounted (admin screens).
  if (!document.getElementById("exec-form") && !document.querySelector(".js-procedure-item")) {
    return;
  }

  const hasProcedure = hasSelectedProcedure();

  // Same native disabled behavior as Execute (faded + not-allowed cursor).
  ["btn-execute", "btn-clear"].forEach((id) => {
    const btn = document.getElementById(id);
    if (!btn) return;
    btn.disabled = !hasProcedure;
    btn.setAttribute("aria-disabled", hasProcedure ? "false" : "true");
  });

  // Export only after a successful Execute with rows.
  setExportEnabled(hasProcedure && hasExportableResults());

  const hint = document.getElementById("procedure-selection-hint");
  if (hint) {
    hint.classList.toggle("hidden", hasProcedure);
  }
}

// Clear resets the home screen when a procedure is selected.
document.body.addEventListener("click", (e) => {
  const clear = e.target.closest?.("#btn-clear");
  if (!clear || clear.disabled) return;

  if (!hasSelectedProcedure()) {
    e.preventDefault();
    updateHomeActionButtons();
    return;
  }

  const url = clear.getAttribute("data-clear-url") || "/";
  window.location.href = url;
});
function clearExportableResults() {
  destroyResultsClusterize();
  const panel = document.getElementById("results-panel");
  if (panel) {
    panel.innerHTML =
      '<div class="js-results-root" data-export-ready="false" data-row-count="0"><p class="qp-results-empty text-xs text-slate-500"></p></div>';
  }
  const status = document.getElementById("export-status");
  if (status) status.innerHTML = "";
  setExportEnabled(false);
}

function wireHomeProcedureGuards() {
  const form = document.getElementById("exec-form");
  if (!form && !document.querySelector(".js-procedure-item")) return;

  // Outlook-style list: click selects procedure, updates hidden id, loads params via HTMX.
  document.body.addEventListener("click", (e) => {
    const item = e.target.closest?.(".js-procedure-item");
    if (!item) return;
    selectProcedureItem(item);
  });

  // Parameter edits after Execute invalidate export eligibility client-side.
  if (form) {
    // Enter in a parameter field would otherwise full-post the form and wipe the UI
    // (POST without a page handler re-renders without loading the procedure list).
    form.addEventListener("submit", (e) => {
      e.preventDefault();
      e.stopPropagation();

      const executeBtn = document.getElementById("btn-execute");
      if (executeBtn && !executeBtn.disabled && hasSelectedProcedure()) {
        // Same path as clicking Execute (HTMX + validation guards).
        executeBtn.click();
      }

      return false;
    });

    form.addEventListener("input", (e) => {
      const t = e.target;
      if (!t || t.id === "procedureId" || t.name === "procedureId") return;
      if (t.name && (t.name.startsWith("param_") || t.name.startsWith("paramcheck_"))) {
        setExportEnabled(false);
        const root = document.querySelector("#results-panel .js-results-root");
        if (root) root.setAttribute("data-export-ready", "false");
      }
    });
    form.addEventListener("change", (e) => {
      const t = e.target;
      if (!t || t.id === "procedureId" || t.name === "procedureId") return;
      if (t.name && (t.name.startsWith("param_") || t.name.startsWith("paramcheck_"))) {
        setExportEnabled(false);
        const root = document.querySelector("#results-panel .js-results-root");
        if (root) root.setAttribute("data-export-ready", "false");
      }
    });
  }

  updateHomeActionButtons();
}

// After Execute swaps results: re-init Clusterize + export eligibility.
document.body.addEventListener("htmx:afterSwap", (event) => {
  if (event.detail.target?.id === "results-panel") {
    const root = event.detail.target.querySelector(".js-results-root");
    if (root) {
      initResultsClusterize(root);
    } else {
      destroyResultsClusterize();
    }
    updateHomeActionButtons();
  }
});

// When the results panel is about to be replaced, tear down Clusterize first.
document.body.addEventListener("htmx:beforeSwap", (event) => {
  if (event.detail.target?.id === "results-panel") {
    destroyResultsClusterize();
  }
});

function clearRequiredParameterErrors() {
  document.querySelectorAll(".js-param-error").forEach((el) => {
    el.classList.add("hidden");
    el.textContent = "";
  });
  document.querySelectorAll(".js-param-input.input-validation-error").forEach((el) => {
    el.classList.remove("input-validation-error");
  });
}

function validateRequiredParameters() {
  clearRequiredParameterErrors();
  const missing = [];

  document.querySelectorAll("#execution-parameters .js-param-field[data-required='true']").forEach((field) => {
    const input = field.querySelector(".js-param-input");
    if (!input) return;

    const value = (input.value || "").trim();
    if (!value) {
      const caption = field.getAttribute("data-caption") || input.name || "Parameter";
      missing.push(caption);
      input.classList.add("input-validation-error");
      const err = field.querySelector(".js-param-error");
      if (err) {
        const page = document.querySelector("[data-msg-required-params]");
        const template =
          page?.getAttribute("data-msg-required-params") ||
          "Required parameter: {0}";
        err.textContent = template.replace("{0}", caption);
        err.classList.remove("hidden");
      }
    }
  });

  return missing;
}

// Block HTMX when guards fail.
document.body.addEventListener("htmx:configRequest", (event) => {
  const elt = event.detail.elt;
  if (!elt) return;

  if (elt.id === "btn-execute") {
    if (!hasSelectedProcedure()) {
      event.preventDefault();
      updateHomeActionButtons();
      const panel = document.getElementById("results-panel");
      if (panel) {
        const root = document.querySelector("[data-msg-select-procedure]");
        const msg =
          root?.getAttribute("data-msg-select-procedure") ||
          "Select a procedure before executing.";
        panel.innerHTML =
          '<div class="js-results-root" data-export-ready="false" data-row-count="0"><div class="rounded border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-900"></div></div>';
        panel.querySelector(".rounded").textContent = msg;
      }
      setExportEnabled(false);
      return;
    }

    const missing = validateRequiredParameters();
    if (missing.length > 0) {
      event.preventDefault();
      setExportEnabled(false);
      const panel = document.getElementById("results-panel");
      if (panel) {
        const page = document.querySelector("[data-msg-required-params]");
        const template =
          page?.getAttribute("data-msg-required-params") ||
          "Fill required parameters: {0}";
        // When template is "Required parameter: {0}", join names for multi.
        const msg =
          missing.length === 1
            ? template.replace("{0}", missing[0])
            : (page?.getAttribute("data-msg-required-params-multi") ||
                "Fill required parameters: {0}").replace("{0}", missing.join(", "));
        panel.innerHTML =
          '<div class="js-results-root" data-export-ready="false" data-row-count="0"><div class="rounded border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-900"></div></div>';
        panel.querySelector(".rounded").textContent = msg;
      }
    }
    return;
  }

  if (elt.id === "btn-export") {
    if (!hasSelectedProcedure() || !hasExportableResults()) {
      event.preventDefault();
      updateHomeActionButtons();
      const status = document.getElementById("export-status");
      if (status) {
        const page = document.querySelector("[data-msg-export-requires-data]");
        const msg =
          page?.getAttribute("data-msg-export-requires-data") ||
          "Execute a procedure that returns data before exporting.";
        status.innerHTML = '<span class="text-sm text-red-700"></span>';
        status.firstElementChild.textContent = msg;
      }
    }
  }
});

document.addEventListener("DOMContentLoaded", wireHomeProcedureGuards);

// Maximize / restore the results grid (hides procedure list + parameters).
const RESULTS_MAX_STORAGE_KEY = "qp-home-results-maximized";

function isResultsMaximized() {
  return !!document.querySelector(".qp-home-columns.is-results-maximized");
}

function setResultsMaximized(maximized) {
  const columns = document.querySelector(".qp-home-columns");
  const btn = document.getElementById("btn-toggle-results-max");
  if (!columns) return;

  columns.classList.toggle("is-results-maximized", maximized);

  if (btn) {
    const labelMaximize =
      btn.getAttribute("data-label-maximize") || "Maximize";
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
    sessionStorage.setItem(RESULTS_MAX_STORAGE_KEY, maximized ? "1" : "0");
  } catch {
    // private mode / blocked storage — ignore
  }
}

function wireResultsMaximize() {
  const btn = document.getElementById("btn-toggle-results-max");
  const columns = document.querySelector(".qp-home-columns");
  if (!btn || !columns) return;

  let initial = false;
  try {
    initial = sessionStorage.getItem(RESULTS_MAX_STORAGE_KEY) === "1";
  } catch {
    initial = false;
  }

  setResultsMaximized(initial);

  btn.addEventListener("click", (e) => {
    e.preventDefault();
    setResultsMaximized(!isResultsMaximized());
    // Layout width changes — refresh Clusterize row metrics + header sync.
    requestAnimationFrame(() => {
      if (resultsGridState?.root) {
        applyColumnWidths(resultsGridState.root, resultsGridState.columns);
        syncResultsHeaderScroll(resultsGridState.root);
        try {
          resultsClusterize?.refresh(true);
        } catch {
          // ignore
        }
      }
    });
  });
}

document.addEventListener("DOMContentLoaded", wireResultsMaximize);

// Enable "Sync metadata" only when Database + Procedure name are filled.
function updateSyncMetadataButtonState(root) {
  const scope = root || document;
  const btn = scope.querySelector?.("#btn-sync-metadata") || document.getElementById("btn-sync-metadata");
  if (!btn) return;

  const form = btn.closest("form") || document;
  const database =
    form.querySelector('[data-sync-field="database"]') ||
    form.querySelector('input[name="Input.DatabaseName"]');
  const procedure =
    form.querySelector('[data-sync-field="procedure"]') ||
    form.querySelector('input[name="Input.ProcedureName"]');

  const canSync =
    !!(database?.value || "").trim() && !!(procedure?.value || "").trim();

  btn.disabled = !canSync;
  btn.setAttribute("aria-disabled", canSync ? "false" : "true");

  const hint = document.getElementById("sync-metadata-hint");
  if (hint) {
    hint.classList.toggle("hidden", canSync);
  }
}

function wireSyncMetadataGuards() {
  const sources = document.querySelectorAll(".js-sync-metadata-source");
  if (!sources.length) {
    updateSyncMetadataButtonState();
    return;
  }

  sources.forEach((el) => {
    el.addEventListener("input", () => updateSyncMetadataButtonState());
    el.addEventListener("change", () => updateSyncMetadataButtonState());
  });

  updateSyncMetadataButtonState();
}

document.addEventListener("DOMContentLoaded", wireSyncMetadataGuards);

// Block Sync metadata submit if fields were cleared after the button was enabled.
document.body.addEventListener("submit", (e) => {
  const submitter = e.submitter;
  if (!submitter || submitter.id !== "btn-sync-metadata") return;

  const form = e.target;
  const database = form.querySelector('[data-sync-field="database"]');
  const procedure = form.querySelector('[data-sync-field="procedure"]');
  const canSync =
    !!(database?.value || "").trim() && !!(procedure?.value || "").trim();

  if (!canSync) {
    e.preventDefault();
    updateSyncMetadataButtonState(form);
  }
});
