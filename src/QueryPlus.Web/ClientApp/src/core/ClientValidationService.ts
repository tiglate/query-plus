import { injectable, singleton } from "tsyringe";
import { ValidationService } from "aspnet-client-validation";

/**
 * jQuery-free ASP.NET unobtrusive client validation (data-val-* attributes).
 * Uses https://github.com/haacked/aspnet-client-validation
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
    this.service.bootstrap({
      root,
      watch: true,
      addNoValidate: true,
    });
  }

  dispose(): void {
    // Library has no public teardown; drop reference so a remount can recreate.
    this.service = null;
  }
}
