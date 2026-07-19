/**
 * Base for page / shell controllers mounted by bootstrap.
 */
export abstract class PageController {
  abstract mount(root: ParentNode): void;

  unmount(): void {
    // default: nothing
  }

  dispose(): void {
    this.unmount();
  }
}
