import { inject, injectable, singleton } from "tsyringe";
import { TOKENS } from "./di/tokens";

/** Minimal shape of htmx configRequest detail we care about. */
export interface HtmxConfigRequestDetail {
  headers: Record<string, string>;
}

export type HtmxConfigRequestHandler = (detail: HtmxConfigRequestDetail) => void;

/**
 * Thin wrapper around document-level HTMX events.
 * Keeps CSRF and future swap handlers out of inline layout scripts.
 */
@singleton()
@injectable()
export class HtmxBridge {
  private readonly unsubscribers: Array<() => void> = [];

  constructor(@inject(TOKENS.Document) private readonly doc: Document) {}

  /**
   * Attach CSRF token from meta[name=csrf-token] on every htmx:configRequest.
   */
  wireCsrfFromMeta(metaName = "csrf-token"): void {
    this.onConfigRequest((detail) => {
      const token = this.doc
        .querySelector(`meta[name="${metaName}"]`)
        ?.getAttribute("content");
      if (token) {
        detail.headers["RequestVerificationToken"] = token;
      }
    });
  }

  onConfigRequest(handler: HtmxConfigRequestHandler): () => void {
    const listener = (event: Event) => {
      const custom = event as CustomEvent<HtmxConfigRequestDetail>;
      if (custom.detail?.headers) {
        handler(custom.detail);
      }
    };
    this.doc.body.addEventListener("htmx:configRequest", listener);
    const off = () =>
      this.doc.body.removeEventListener("htmx:configRequest", listener);
    this.unsubscribers.push(off);
    return off;
  }

  onAfterSwap(handler: (event: Event) => void): () => void {
    const listener = (event: Event) => handler(event);
    this.doc.body.addEventListener("htmx:afterSwap", listener);
    const off = () =>
      this.doc.body.removeEventListener("htmx:afterSwap", listener);
    this.unsubscribers.push(off);
    return off;
  }

  onBeforeSwap(handler: (event: Event) => void): () => void {
    const listener = (event: Event) => handler(event);
    this.doc.body.addEventListener("htmx:beforeSwap", listener);
    const off = () =>
      this.doc.body.removeEventListener("htmx:beforeSwap", listener);
    this.unsubscribers.push(off);
    return off;
  }

  dispose(): void {
    while (this.unsubscribers.length) {
      this.unsubscribers.pop()?.();
    }
  }
}
