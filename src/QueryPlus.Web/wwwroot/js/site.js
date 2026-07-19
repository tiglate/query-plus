// Lightweight helpers for HTMX-driven UI (no SPA framework).
document.addEventListener("DOMContentLoaded", () => {
  // Client-side sort for result grids: click on th[data-sort-col]
  document.body.addEventListener("click", (e) => {
    const th = e.target.closest("th[data-sort-col]");
    if (!th) return;
    const table = th.closest("table");
    if (!table) return;
    const colIndex = Number(th.dataset.sortCol);
    const tbody = table.querySelector("tbody");
    if (!tbody) return;

    const rows = Array.from(tbody.querySelectorAll("tr"));
    const asc = th.dataset.sortDir !== "asc";
    th.dataset.sortDir = asc ? "asc" : "desc";

    rows.sort((a, b) => {
      const av = (a.children[colIndex]?.textContent || "").trim();
      const bv = (b.children[colIndex]?.textContent || "").trim();
      const an = Number(av.replace(",", "."));
      const bn = Number(bv.replace(",", "."));
      if (!Number.isNaN(an) && !Number.isNaN(bn) && av !== "" && bv !== "") {
        return asc ? an - bn : bn - an;
      }
      return asc ? av.localeCompare(bv) : bv.localeCompare(av);
    });

    rows.forEach((r) => tbody.appendChild(r));
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
  const panel = document.getElementById("results-panel");
  if (panel) {
    panel.innerHTML =
      '<div class="js-results-root" data-export-ready="false" data-row-count="0"><p class="text-sm text-slate-500"></p></div>';
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

// After Execute swaps results, enable Export only when data-export-ready=true.
document.body.addEventListener("htmx:afterSwap", (event) => {
  if (event.detail.target?.id === "results-panel") {
    updateHomeActionButtons();
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
