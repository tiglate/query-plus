import { injectable, singleton } from "tsyringe";
import { ValidationService } from "aspnet-client-validation";

/**
 * jQuery-free ASP.NET unobtrusive client validation (data-val-* attributes).
 * Uses https://github.com/haacked/aspnet-client-validation
 *
 * Note: the library's default success path re-dispatches a cancelable `submit`
 * event before calling form.submit(). Global submit handlers (or a second
 * validation pass) can cancel that event, so the browser never posts and the
 * user must click Save again. We override submitValidForm to post natively
 * after validation succeeds.
 */
@singleton()
@injectable()
export class ClientValidationService {
    private service: ValidationService | null = null;

    /**
     * Scan the document for data-val rules and wire form/input validation.
     * `watch: true` re-scans when the DOM mutates (e.g. HTMX partials).
     */
    mount(root: ParentNode = document): void {
        if (this.service) return;
        this.service = new ValidationService();

        // Post without re-dispatching submit (avoids double-handling / swallowed posts).
        this.service.submitValidForm = (form, submitEvent) => {
            submitFormNatively(form, submitEvent);
        };

        this.service.bootstrap({
            root,
            watch: true,
            addNoValidate: true,
        });
    }

    dispose(): void {
        this.service = null;
    }
}

/**
 * Equivalent to the library's submitValidForm body, but always posts via the
 * native HTMLFormElement.prototype.submit (no second submit event).
 */
function submitFormNatively(form: HTMLFormElement, submitEvent?: SubmitEvent): void {
    const submitter = submitEvent?.submitter ?? null;
    let submitterInput: HTMLInputElement | null = null;
    const initialFormAction = form.action;

    if (submitter instanceof HTMLElement) {
        const name = submitter.getAttribute("name");
        if (name) {
            submitterInput = document.createElement("input");
            submitterInput.type = "hidden";
            submitterInput.name = name;
            submitterInput.value = submitter.getAttribute("value") ?? "";
            form.appendChild(submitterInput);
        }
        const formAction = submitter.getAttribute("formaction");
        if (formAction) {
            form.action = formAction;
        }
    }

    try {
        // Prototype call avoids issues if a control is named "submit".
        HTMLFormElement.prototype.submit.call(form);
    } finally {
        if (submitterInput) {
            form.removeChild(submitterInput);
        }
        form.action = initialFormAction;
    }
}
