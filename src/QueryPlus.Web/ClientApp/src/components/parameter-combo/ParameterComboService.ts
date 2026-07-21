import { inject, injectable, singleton } from "tsyringe";
import { TOKENS } from "../../core/di/tokens";
import { applyComboVisibility } from "./parameterCombo";

@singleton()
@injectable()
export class ParameterComboService {
  private readonly disposers: Array<() => void> = [];

  constructor(@inject(TOKENS.Document) private readonly doc: Document) {}

  /**
   * Wire all .js-param-row under scope (default document).
   * Idempotent per row via data-combo-wired.
   */
  mountAll(scope: ParentNode = this.doc): void {
    scope.querySelectorAll<HTMLElement>(".js-param-row").forEach((row) => {
      this.wireRow(row);
    });
  }

  wireRow(row: HTMLElement): void {
    const typeSelect = row.querySelector<HTMLSelectElement>(".js-param-type");
    const comboInput = row.querySelector<HTMLInputElement>(".js-param-combo-values");
    applyComboVisibility(typeSelect, comboInput);

    if (!typeSelect || typeSelect.dataset.comboWired === "1") return;
    typeSelect.dataset.comboWired = "1";

    const onChange = () => applyComboVisibility(typeSelect, comboInput);
    typeSelect.addEventListener("change", onChange);
    this.disposers.push(() => {
      typeSelect.removeEventListener("change", onChange);
      delete typeSelect.dataset.comboWired;
    });
  }

  dispose(): void {
    while (this.disposers.length) {
      this.disposers.pop()?.();
    }
  }
}
