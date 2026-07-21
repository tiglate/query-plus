import { inject, injectable, singleton } from "tsyringe";
import { TOKENS, type ConfirmFn } from "@/core/di/tokens";

/**
 * Intercepts form submit when form[data-confirm] is set; cancels if user declines.
 *
 * Uses capture phase so we run before form-level handlers (e.g. aspnet-client-validation).
 * After the user accepts, marks the form so a synthetic re-dispatched submit
 * (validation library) does not prompt a second time.
 */
@singleton()
@injectable()
export class ConfirmSubmitService {
    private bound = false;
    private readonly onSubmit: (event: Event) => void;

    constructor(
        @inject(TOKENS.Document) private readonly doc: Document,
        @inject(TOKENS.ConfirmFn) private readonly confirmFn: ConfirmFn,
    ) {
        this.onSubmit = (event: Event) => {
            const form = resolveSubmitForm(event);
            if (!form) return;

            const message = form.dataset.confirm;
            if (!message) return;

            // Already confirmed earlier in this submit chain (or by another listener).
            if (form.dataset.qpConfirmOk === "1") {
                delete form.dataset.qpConfirmOk;
                return;
            }

            if (!this.confirmFn(message)) {
                event.preventDefault();
                event.stopImmediatePropagation();
                return;
            }

            // Allow one follow-up submit (e.g. aspnet-client-validation re-dispatch) without re-prompting.
            form.dataset.qpConfirmOk = "1";
        };
    }

    mount(scope: ParentNode = this.doc): void {
        if (this.bound) return;
        const body = scope instanceof Document ? scope.body : (this.doc.body as HTMLElement);
        // Capture: run before form-level validation listeners.
        body.addEventListener("submit", this.onSubmit, true);
        this.bound = true;
    }

    dispose(): void {
        if (!this.bound) return;
        this.doc.body.removeEventListener("submit", this.onSubmit, true);
        this.bound = false;
    }
}

function resolveSubmitForm(event: Event): HTMLFormElement | null {
    const t = event.target;
    if (t instanceof HTMLFormElement) return t;
    if (t instanceof Element) return t.closest("form");
    return null;
}
