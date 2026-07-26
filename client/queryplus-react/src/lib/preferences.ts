export const THEME_KEY = "qp-theme";
export const FONT_SIZE_KEY = "qp-font-size-step";
export const FONT_STEPS = [14, 15, 16, 17, 18, 19, 20] as const;
export const DEFAULT_FONT_STEP = 3;

export type Theme = "light" | "dark" | "system";

export function applyTheme(theme: Theme): void {
    const dark =
        theme === "dark" ||
        (theme === "system" && window.matchMedia("(prefers-color-scheme: dark)").matches);
    document.documentElement.classList.toggle("dark", dark);
    document.documentElement.style.colorScheme = dark ? "dark" : "light";
    document.documentElement.dataset.theme = theme;
    document.documentElement.dataset.themeResolved = dark ? "light" : "dark";
    localStorage.setItem(THEME_KEY, theme);
}

export function changeFontSize(delta: number): number {
    const stored = document.documentElement.dataset.fontSizeStep;
    const parsed = stored === null || stored === undefined ? Number.NaN : Number(stored);
    const current = Number.isFinite(parsed) ? parsed : DEFAULT_FONT_STEP;
    const step = Math.min(FONT_STEPS.length - 1, Math.max(0, Math.round(current) + delta));
    document.documentElement.style.fontSize = `${FONT_STEPS[step]}px`;
    document.documentElement.dataset.fontSizeStep = String(step);
    localStorage.setItem(FONT_SIZE_KEY, String(step));
    window.dispatchEvent(new CustomEvent("qp-font-size-change"));
    return step;
}
