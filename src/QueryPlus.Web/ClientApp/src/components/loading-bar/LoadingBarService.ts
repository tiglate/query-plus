import { inject, injectable, singleton } from "tsyringe";
import { HtmxBridge } from "@/core/HtmxBridge";
import { TOKENS } from "@/core/di/tokens";

const INDICATOR_SELECTOR = "#qp-loading-bar";
const ACTIVE_CLASS = "is-active";

/** Elements (or ancestors) with this attribute never trigger the global bar. */
const SKIP_ATTR = "data-loading-indicator";
const SKIP_VALUE = "skip";

/**
 * Global slim activity bar reacting to every htmx request, so any part of
 * the app that talks to the server shows visible activity on a slow day —
 * independent of whether the triggering element has its own hx-indicator.
 *
 * Elements that already have their own local "in progress" affordance (e.g.
 * background polling) can opt out via data-loading-indicator="skip".
 */
@singleton()
@injectable()
export class LoadingBarService {
  private el: HTMLElement | null = null;
  private inFlight = 0;
  private readonly disposers: Array<() => void> = [];

  constructor(
    @inject(TOKENS.Document) private readonly doc: Document,
    @inject(HtmxBridge) private readonly htmx: HtmxBridge,
  ) {}

  mount(root: ParentNode = this.doc): void {
    this.dispose();

    this.el =
      root.querySelector<HTMLElement>(INDICATOR_SELECTOR) ??
      this.doc.querySelector<HTMLElement>(INDICATOR_SELECTOR);
    this.inFlight = 0;

    this.disposers.push(
      this.htmx.onBeforeRequest((event) => this.handleStart(event)),
      this.htmx.onAfterRequest((event) => this.handleEnd(event)),
    );
  }

  dispose(): void {
    while (this.disposers.length) {
      this.disposers.pop()?.();
    }
    this.inFlight = 0;
    this.el?.classList.remove(ACTIVE_CLASS);
  }

  private handleStart(event: Event): void {
    if (this.shouldSkip(event)) return;
    this.inFlight += 1;
    if (this.inFlight === 1) {
      this.el?.classList.add(ACTIVE_CLASS);
    }
  }

  private handleEnd(event: Event): void {
    if (this.shouldSkip(event)) return;
    this.inFlight = Math.max(0, this.inFlight - 1);
    if (this.inFlight === 0) {
      this.el?.classList.remove(ACTIVE_CLASS);
    }
  }

  private shouldSkip(event: Event): boolean {
    const elt = (event as CustomEvent<{ elt?: unknown }>).detail?.elt;
    return elt instanceof Element && elt.closest(`[${SKIP_ATTR}="${SKIP_VALUE}"]`) !== null;
  }
}
