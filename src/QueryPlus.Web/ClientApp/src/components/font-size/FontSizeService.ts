import { inject, injectable, singleton } from "tsyringe";
import { TOKENS } from "@/core/di/tokens";
import {
    applyFontSizeStep,
    clampFontSizeStep,
    MAX_FONT_SIZE_STEP,
    MIN_FONT_SIZE_STEP,
    readFontSizeStep,
    writeFontSizeStep,
} from "./fontSize";

const DECREASE_SELECTOR = "[data-font-size-decrease]";
const INCREASE_SELECTOR = "[data-font-size-increase]";

/**
 * Header font-size stepper: persists preference in localStorage and
 * scales the whole app (rem-based Tailwind type scale) via <html> font-size.
 */
@singleton()
@injectable()
export class FontSizeService {
    private unsub: (() => void) | null = null;

    constructor(
        @inject(TOKENS.Document) private readonly doc: Document,
        @inject(TOKENS.Window) private readonly win: Window,
    ) {}

    mount(root: ParentNode = this.doc): void {
        this.dispose();

        const step = readFontSizeStep(this.win.localStorage);
        this.apply(step, root);

        const onClick = (event: Event) => {
            const target = event.target;
            if (!(target instanceof Element)) return;

            const decreaseBtn = target.closest(DECREASE_SELECTOR);
            const increaseBtn = target.closest(INCREASE_SELECTOR);
            if (!decreaseBtn && !increaseBtn) return;

            const current = readFontSizeStep(this.win.localStorage);
            const next = clampFontSizeStep(current + (increaseBtn ? 1 : -1));
            if (next === current) return;

            writeFontSizeStep(this.win.localStorage, next);
            this.apply(next);
        };

        // Capture on document so we work even if the buttons are re-rendered.
        this.doc.addEventListener("click", onClick);
        this.unsub = () => this.doc.removeEventListener("click", onClick);
    }

    dispose(): void {
        this.unsub?.();
        this.unsub = null;
    }

    private apply(step: number, root: ParentNode = this.doc): void {
        applyFontSizeStep(this.doc.documentElement, step);
        this.syncButtons(step, root);
    }

    private syncButtons(step: number, root: ParentNode): void {
        const decreaseBtn =
            root.querySelector(DECREASE_SELECTOR) ?? this.doc.querySelector(DECREASE_SELECTOR);
        const increaseBtn =
            root.querySelector(INCREASE_SELECTOR) ?? this.doc.querySelector(INCREASE_SELECTOR);
        if (decreaseBtn instanceof HTMLButtonElement) {
            decreaseBtn.disabled = step <= MIN_FONT_SIZE_STEP;
        }
        if (increaseBtn instanceof HTMLButtonElement) {
            increaseBtn.disabled = step >= MAX_FONT_SIZE_STEP;
        }
    }
}
