// Lightweight helpers still on legacy site.js (admin only after Phase 3).
// Home execution UI: ClientApp HomePageController (data-page="home" via dist/js/app.js).

document.addEventListener("DOMContentLoaded", wireParameterComboVisibility);
document.addEventListener("DOMContentLoaded", wireSyncMetadataGuards);

/**
 * Procedure edit form: show Combo values only when ParameterType is Combo.
 * Disables the input when hidden so non-combo values are not posted.
 */
function updateParameterComboVisibility(row) {
  if (!row) return;
  const typeSelect = row.querySelector(".js-param-type");
  const comboInput = row.querySelector(".js-param-combo-values");
  if (!typeSelect || !comboInput) return;

  const comboTypeValue =
    comboInput.getAttribute("data-combo-type-value") || "6";
  const isCombo = String(typeSelect.value) === String(comboTypeValue);

  // Non-combo: hide + disable (not posted). Combo: show (readonly still applies on View).
  comboInput.disabled = !isCombo;
  comboInput.classList.toggle("hidden", !isCombo);
}

function wireParameterComboVisibility() {
  document.querySelectorAll(".js-param-row").forEach((row) => {
    updateParameterComboVisibility(row);
    const typeSelect = row.querySelector(".js-param-type");
    if (!typeSelect || typeSelect.dataset.comboWired === "1") return;
    typeSelect.dataset.comboWired = "1";
    typeSelect.addEventListener("change", () => updateParameterComboVisibility(row));
  });
}

// Enable "Sync metadata" only when Database + Procedure name are filled.
function updateSyncMetadataButtonState(root) {
  const scope = root || document;
  const btn =
    scope.querySelector?.("#btn-sync-metadata") ||
    document.getElementById("btn-sync-metadata");
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
