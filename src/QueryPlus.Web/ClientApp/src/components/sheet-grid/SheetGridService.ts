import { inject, injectable, singleton } from "tsyringe";
import { TOKENS } from "@/core/di/tokens";
import { SheetGrid } from "./SheetGrid";
import type { QueryPlusSheetGridApi, SheetGridState } from "./types";

/**
 * DI-managed registry of sheet grids. Also exposes the legacy global API shape.
 */
@singleton()
@injectable()
export class SheetGridService implements QueryPlusSheetGridApi {
  private readonly instances = new WeakMap<Element, SheetGrid>();

  constructor(@inject(TOKENS.Window) private readonly win: Window) {}

  mount(root: Element | null | undefined): SheetGridState | null {
    if (!(root instanceof HTMLElement)) return null;
    let grid = this.instances.get(root);
    if (!grid) {
      grid = new SheetGrid(root, this.win);
      this.instances.set(root, grid);
    }
    return grid.mount();
  }

  mountAll(selector: string): void {
    this.win.document.querySelectorAll(selector).forEach((el) => {
      this.mount(el);
    });
  }

  destroy(root: Element): void {
    const grid = this.instances.get(root);
    if (!grid) return;
    grid.destroy();
    this.instances.delete(root);
  }

  refresh(root: Element): void {
    this.instances.get(root)?.refresh();
  }

  getState(root: Element): SheetGridState | null {
    return this.instances.get(root)?.getState() ?? null;
  }

  /** Install window.QueryPlusSheetGrid for any non-DI callers. */
  installGlobalBridge(): void {
    this.win.QueryPlusSheetGrid = {
      mount: (root) => this.mount(root),
      mountAll: (selector) => this.mountAll(selector),
      destroy: (root) => this.destroy(root),
      refresh: (root) => this.refresh(root),
      getState: (root) => this.getState(root),
    };
  }
}
