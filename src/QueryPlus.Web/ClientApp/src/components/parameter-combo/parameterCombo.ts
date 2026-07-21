/**
 * Pure helpers for procedure parameter combo-values visibility.
 */

export const DEFAULT_COMBO_TYPE_VALUE = "6";

export function isComboType(
    typeValue: string | null | undefined,
    comboTypeValue: string = DEFAULT_COMBO_TYPE_VALUE,
): boolean {
    return String(typeValue ?? "") === String(comboTypeValue);
}

/**
 * Apply show/hide + disabled state to a combo values input for a parameter row.
 * Returns whether the field is in combo mode.
 */
export function applyComboVisibility(
    typeSelect: HTMLSelectElement | HTMLInputElement | null | undefined,
    comboInput: HTMLInputElement | HTMLTextAreaElement | null | undefined,
): boolean {
    if (!typeSelect || !comboInput) return false;

    const comboTypeValue =
        comboInput.getAttribute("data-combo-type-value") || DEFAULT_COMBO_TYPE_VALUE;
    const isCombo = isComboType(typeSelect.value, comboTypeValue);

    comboInput.disabled = !isCombo;
    comboInput.classList.toggle("hidden", !isCombo);
    return isCombo;
}
