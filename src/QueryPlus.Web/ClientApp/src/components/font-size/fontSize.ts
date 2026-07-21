export const FONT_SIZE_STORAGE_KEY = "qp-font-size-step";

/** Root font-size options (px). Index into this array is what gets persisted. */
export const FONT_SIZE_STEPS_PX = [12, 13, 14, 15, 16, 17, 18] as const;

/** Matches the html { font-size: 14px } default in base.css. */
export const DEFAULT_FONT_SIZE_STEP = 2;

export const MIN_FONT_SIZE_STEP = 0;
export const MAX_FONT_SIZE_STEP = FONT_SIZE_STEPS_PX.length - 1;

export function clampFontSizeStep(step: number): number {
    if (!Number.isFinite(step)) return DEFAULT_FONT_SIZE_STEP;
    return Math.min(MAX_FONT_SIZE_STEP, Math.max(MIN_FONT_SIZE_STEP, Math.trunc(step)));
}

export function readFontSizeStep(storage: Pick<Storage, "getItem"> | null | undefined): number {
    try {
        const raw = storage?.getItem(FONT_SIZE_STORAGE_KEY);
        if (raw === null || raw === undefined) return DEFAULT_FONT_SIZE_STEP;
        const parsed = Number(raw);
        return Number.isFinite(parsed) ? clampFontSizeStep(parsed) : DEFAULT_FONT_SIZE_STEP;
    } catch {
        // private mode / blocked storage
        return DEFAULT_FONT_SIZE_STEP;
    }
}

export function writeFontSizeStep(
    storage: Pick<Storage, "setItem"> | null | undefined,
    step: number,
): void {
    try {
        storage?.setItem(FONT_SIZE_STORAGE_KEY, String(clampFontSizeStep(step)));
    } catch {
        // private mode / blocked storage
    }
}

/** Applies the resolved px size to <html> (higher specificity than the base.css default). */
export function applyFontSizeStep(root: HTMLElement, step: number): void {
    const clamped = clampFontSizeStep(step);
    root.style.fontSize = `${FONT_SIZE_STEPS_PX[clamped]}px`;
    root.dataset.fontSizeStep = String(clamped);
}
