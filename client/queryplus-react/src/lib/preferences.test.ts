import { afterEach, beforeEach, expect, test, vi } from "vitest";
import {
    applyTheme,
    changeFontSize,
    DEFAULT_FONT_STEP,
    FONT_SIZE_KEY,
    FONT_STEPS,
    THEME_KEY,
} from "./preferences";

beforeEach(() => {
    document.documentElement.className = "";
    document.documentElement.removeAttribute("style");
    delete document.documentElement.dataset.theme;
    delete document.documentElement.dataset.themeResolved;
    delete document.documentElement.dataset.fontSizeStep;
});

afterEach(() => {
    vi.restoreAllMocks();
});

test("applyTheme('dark') marks the document dark and persists the choice", () => {
    applyTheme("dark");

    expect(document.documentElement.classList.contains("dark")).toBe(true);
    expect(document.documentElement.style.colorScheme).toBe("dark");
    expect(document.documentElement.dataset.theme).toBe("dark");
    expect(localStorage.getItem(THEME_KEY)).toBe("dark");
});

test("applyTheme('light') marks the document light and persists the choice", () => {
    applyTheme("light");

    expect(document.documentElement.classList.contains("dark")).toBe(false);
    expect(document.documentElement.style.colorScheme).toBe("light");
    expect(localStorage.getItem(THEME_KEY)).toBe("light");
});

test("applyTheme('system') follows the OS preference reported by matchMedia", () => {
    vi.spyOn(window, "matchMedia").mockReturnValue({
        matches: true,
        media: "(prefers-color-scheme: dark)",
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
    } as unknown as MediaQueryList);

    applyTheme("system");

    expect(document.documentElement.classList.contains("dark")).toBe(true);
    expect(document.documentElement.dataset.theme).toBe("system");
    expect(localStorage.getItem(THEME_KEY)).toBe("system");
});

test("changeFontSize increases the step and clamps at the top of FONT_STEPS", () => {
    document.documentElement.dataset.fontSizeStep = String(FONT_STEPS.length - 1);

    const step = changeFontSize(1);

    expect(step).toBe(FONT_STEPS.length - 1);
    expect(document.documentElement.style.fontSize).toBe(`${FONT_STEPS[FONT_STEPS.length - 1]}px`);
    expect(localStorage.getItem(FONT_SIZE_KEY)).toBe(String(FONT_STEPS.length - 1));
});

test("changeFontSize decreases the step and clamps at the bottom of FONT_STEPS", () => {
    document.documentElement.dataset.fontSizeStep = "0";

    const step = changeFontSize(-1);

    expect(step).toBe(0);
    expect(document.documentElement.style.fontSize).toBe(`${FONT_STEPS[0]}px`);
});

test("changeFontSize falls back to the default step when nothing is stored yet", () => {
    const step = changeFontSize(1);

    expect(step).toBe(DEFAULT_FONT_STEP + 1);
});

test("changeFontSize dispatches a qp-font-size-change event", () => {
    const listener = vi.fn();
    window.addEventListener("qp-font-size-change", listener);

    changeFontSize(1);

    expect(listener).toHaveBeenCalledTimes(1);
});
