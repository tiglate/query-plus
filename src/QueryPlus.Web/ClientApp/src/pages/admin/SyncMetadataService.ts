import { inject, injectable, singleton } from "tsyringe";
import { TOKENS } from "@/core/di/tokens";
import { canSyncMetadata } from "./syncMetadata";

@singleton()
@injectable()
export class SyncMetadataService {
  private readonly disposers: Array<() => void> = [];

  constructor(@inject(TOKENS.Document) private readonly doc: Document) {}

  updateButtonState(root: ParentNode | Document = this.doc): void {
    const scope = root instanceof Document ? root : root;
    const btn =
      (scope as ParentNode).querySelector?.("#btn-sync-metadata") ||
      this.doc.getElementById("btn-sync-metadata");
    if (!(btn instanceof HTMLButtonElement) && !(btn instanceof HTMLElement)) {
      return;
    }

    const form =
      (btn.closest("form") as HTMLFormElement | null) ||
      (this.doc.querySelector("form") as HTMLFormElement | null) ||
      null;

    const searchRoot: ParentNode = form ?? this.doc;
    const database =
      searchRoot.querySelector<HTMLInputElement>('[data-sync-field="database"]') ||
      searchRoot.querySelector<HTMLInputElement>('input[name="Input.DatabaseName"]');
    const procedure =
      searchRoot.querySelector<HTMLInputElement>('[data-sync-field="procedure"]') ||
      searchRoot.querySelector<HTMLInputElement>('input[name="Input.ProcedureName"]');

    const canSync = canSyncMetadata(database?.value, procedure?.value);

    if (btn instanceof HTMLButtonElement) {
      btn.disabled = !canSync;
    }
    btn.setAttribute("aria-disabled", canSync ? "false" : "true");

    const hint = this.doc.getElementById("sync-metadata-hint");
    if (hint) {
      hint.classList.toggle("hidden", canSync);
    }
  }

  mount(scope: ParentNode = this.doc): void {
    this.dispose();

    const sources = scope.querySelectorAll(".js-sync-metadata-source");
    if (!sources.length) {
      this.updateButtonState(scope);
    } else {
      sources.forEach((el) => {
        const onChange = () => this.updateButtonState(scope);
        el.addEventListener("input", onChange);
        el.addEventListener("change", onChange);
        this.disposers.push(() => {
          el.removeEventListener("input", onChange);
          el.removeEventListener("change", onChange);
        });
      });
      this.updateButtonState(scope);
    }

    const onSubmit = (e: Event) => {
      const submitter = (e as SubmitEvent).submitter;
      if (!submitter || submitter.id !== "btn-sync-metadata") return;

      const form = e.target;
      if (!(form instanceof HTMLFormElement)) return;

      const database = form.querySelector<HTMLInputElement>('[data-sync-field="database"]');
      const procedure = form.querySelector<HTMLInputElement>('[data-sync-field="procedure"]');

      if (!canSyncMetadata(database?.value, procedure?.value)) {
        e.preventDefault();
        this.updateButtonState(form);
      }
    };

    this.doc.body.addEventListener("submit", onSubmit);
    this.disposers.push(() => this.doc.body.removeEventListener("submit", onSubmit));
  }

  dispose(): void {
    while (this.disposers.length) {
      this.disposers.pop()?.();
    }
  }
}
