import { inject, injectable, singleton } from "tsyringe";
import { TOKENS } from "../../core/di/tokens";
import { NavDropdown } from "./NavDropdown";

@singleton()
@injectable()
export class NavDropdownService {
  private instances: NavDropdown[] = [];

  constructor(@inject(TOKENS.Document) private readonly doc: Document) {}

  /**
   * Mount all [data-nav-dropdown] roots under `scope` (default: document).
   */
  mountAll(scope: ParentNode = this.doc): void {
    this.dispose();
    const roots = scope.querySelectorAll<HTMLElement>("[data-nav-dropdown]");
    roots.forEach((root) => {
      const trigger = root.querySelector<HTMLElement>("[data-nav-dropdown-trigger]");
      const panel = root.querySelector<HTMLElement>("[data-nav-dropdown-panel]");
      if (!trigger || !panel) return;
      const dropdown = new NavDropdown(root, trigger, panel);
      dropdown.mount();
      this.instances.push(dropdown);
    });
  }

  dispose(): void {
    this.instances.forEach((d) => d.dispose());
    this.instances = [];
  }
}
