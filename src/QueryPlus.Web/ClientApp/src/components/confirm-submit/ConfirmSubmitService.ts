import { inject, injectable, singleton } from "tsyringe";
import { TOKENS, type ConfirmFn } from "../../core/di/tokens";

/**
 * Intercepts form submit when form[data-confirm] is set; cancels if user declines.
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
      const form = event.target;
      if (!(form instanceof HTMLFormElement)) return;
      const message = form.dataset.confirm;
      if (!message) return;
      if (!this.confirmFn(message)) {
        event.preventDefault();
      }
    };
  }

  mount(scope: ParentNode = this.doc): void {
    if (this.bound) return;
    // Use capture=false on body to match previous site.js behavior.
    const body =
      scope instanceof Document ? scope.body : (this.doc.body as HTMLElement);
    body.addEventListener("submit", this.onSubmit);
    this.bound = true;
  }

  dispose(): void {
    if (!this.bound) return;
    this.doc.body.removeEventListener("submit", this.onSubmit);
    this.bound = false;
  }
}
