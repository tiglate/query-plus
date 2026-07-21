import { afterEach, beforeEach, describe, expect, it } from "vite-plus/test";
import "reflect-metadata";
import { createTestContainer } from "@/core/di/container";
import { FontSizeService } from "@/components/font-size/FontSizeService";
import {
    applyFontSizeStep,
    clampFontSizeStep,
    DEFAULT_FONT_SIZE_STEP,
    FONT_SIZE_STEPS_PX,
    FONT_SIZE_STORAGE_KEY,
    MAX_FONT_SIZE_STEP,
    MIN_FONT_SIZE_STEP,
    readFontSizeStep,
    writeFontSizeStep,
} from "@/components/font-size/fontSize";

describe("fontSize helpers", () => {
    it("clamps out-of-range and non-finite values to the nearest bound / default", () => {
        expect(clampFontSizeStep(-5)).toBe(MIN_FONT_SIZE_STEP);
        expect(clampFontSizeStep(999)).toBe(MAX_FONT_SIZE_STEP);
        expect(clampFontSizeStep(Number.NaN)).toBe(DEFAULT_FONT_SIZE_STEP);
        expect(clampFontSizeStep(1.9)).toBe(1);
    });

    it("reads and writes the step from storage", () => {
        const store = new Map<string, string>();
        const storage = {
            getItem: (k: string) => store.get(k) ?? null,
            setItem: (k: string, v: string) => {
                store.set(k, v);
            },
        };
        expect(readFontSizeStep(storage)).toBe(DEFAULT_FONT_SIZE_STEP);
        writeFontSizeStep(storage, 5);
        expect(store.get(FONT_SIZE_STORAGE_KEY)).toBe("5");
        expect(readFontSizeStep(storage)).toBe(5);
    });

    it("falls back to the default for garbage storage values", () => {
        const storage = { getItem: () => "not-a-number" };
        expect(readFontSizeStep(storage)).toBe(DEFAULT_FONT_SIZE_STEP);
    });

    it("applies the resolved px size and dataset to the root element", () => {
        const root = document.createElement("html");
        applyFontSizeStep(root, MAX_FONT_SIZE_STEP);
        expect(root.style.fontSize).toBe(`${FONT_SIZE_STEPS_PX[MAX_FONT_SIZE_STEP]}px`);
        expect(root.dataset.fontSizeStep).toBe(String(MAX_FONT_SIZE_STEP));
    });
});

describe("FontSizeService", () => {
    beforeEach(() => {
        localStorage.clear();
        document.documentElement.style.fontSize = "";
        document.documentElement.removeAttribute("data-font-size-step");
        document.body.innerHTML = `
      <button data-font-size-decrease></button>
      <button data-font-size-increase></button>
    `;
    });

    afterEach(() => {
        document.body.innerHTML = "";
        document.documentElement.style.fontSize = "";
        localStorage.clear();
    });

    it("increases and decreases the step, persisting and clamping at the bounds", () => {
        const c = createTestContainer();
        const service = c.resolve(FontSizeService);
        service.mount();

        const decreaseBtn = document.querySelector(
            "[data-font-size-decrease]",
        ) as HTMLButtonElement;
        const increaseBtn = document.querySelector(
            "[data-font-size-increase]",
        ) as HTMLButtonElement;

        increaseBtn.click();
        expect(localStorage.getItem(FONT_SIZE_STORAGE_KEY)).toBe(
            String(DEFAULT_FONT_SIZE_STEP + 1),
        );
        expect(document.documentElement.style.fontSize).toBe(
            `${FONT_SIZE_STEPS_PX[DEFAULT_FONT_SIZE_STEP + 1]}px`,
        );

        decreaseBtn.click();
        decreaseBtn.click();
        expect(localStorage.getItem(FONT_SIZE_STORAGE_KEY)).toBe(
            String(DEFAULT_FONT_SIZE_STEP - 1),
        );

        // Drive down to the minimum and confirm the decrease button disables there.
        for (let i = 0; i < FONT_SIZE_STEPS_PX.length; i++) decreaseBtn.click();
        expect(localStorage.getItem(FONT_SIZE_STORAGE_KEY)).toBe(String(MIN_FONT_SIZE_STEP));
        expect(decreaseBtn.disabled).toBe(true);
        expect(increaseBtn.disabled).toBe(false);

        service.dispose();
    });
});
