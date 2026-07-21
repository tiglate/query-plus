export type TimeoutId = ReturnType<typeof setTimeout>;

export interface NavDropdownOptions {
  /** Delay before closing after pointer/focus leave (ms). Default 250. */
  closeDelayMs?: number;
  /**
   * Timer helpers (injectable for tests).
   * Must keep correct `this` binding — never pass bare `setTimeout` without `.bind(window)`.
   */
  setTimeoutFn?: (handler: () => void, timeout: number) => TimeoutId;
  clearTimeoutFn?: (id: TimeoutId) => void;
}

/** Safe wrappers: calling unbound `setTimeout` throws "Illegal invocation" in browsers. */
const defaultSetTimeout = (handler: () => void, timeout: number): TimeoutId =>
  globalThis.setTimeout(handler, timeout);
const defaultClearTimeout = (id: TimeoutId): void => {
  globalThis.clearTimeout(id);
};

/**
 * Single Admin-style nav dropdown: open on hover/focus/click, delayed close
 * so the pointer can cross the gap between trigger and panel.
 */
export class NavDropdown {
  private closeTimer: TimeoutId | null = null;
  /** Blocks open() while Escape focuses the trigger (focusin would re-open). */
  private suppressOpen = false;
  private readonly closeDelayMs: number;
  private readonly setTimeoutFn: (handler: () => void, timeout: number) => TimeoutId;
  private readonly clearTimeoutFn: (id: TimeoutId) => void;
  private readonly disposers: Array<() => void> = [];

  private readonly onOpen: () => void;
  private readonly onClose: () => void;
  private readonly onScheduleClose: () => void;
  private readonly onTriggerClick: (e: Event) => void;
  private readonly onFocusOut: (e: FocusEvent) => void;
  private readonly onDocumentClick: (e: MouseEvent) => void;
  private readonly onDocumentKeydown: (e: KeyboardEvent) => void;

  constructor(
    private readonly root: HTMLElement,
    private readonly trigger: HTMLElement,
    private readonly panel: HTMLElement,
    options: NavDropdownOptions = {},
  ) {
    this.closeDelayMs = options.closeDelayMs ?? 250;
    this.setTimeoutFn = options.setTimeoutFn ?? defaultSetTimeout;
    this.clearTimeoutFn = options.clearTimeoutFn ?? defaultClearTimeout;

    this.onOpen = () => this.open();
    this.onClose = () => this.close();
    this.onScheduleClose = () => this.scheduleClose();
    this.onTriggerClick = (e: Event) => {
      e.preventDefault();
      if (this.panel.hidden) this.open();
      else this.close();
    };
    this.onFocusOut = (e: FocusEvent) => {
      if (!this.root.contains(e.relatedTarget as Node | null)) {
        this.scheduleClose();
      }
    };
    this.onDocumentClick = (e: MouseEvent) => {
      if (!this.root.contains(e.target as Node | null)) {
        this.close();
      }
    };
    this.onDocumentKeydown = (e: KeyboardEvent) => {
      this.handleDocumentKeydown(e);
    };
  }

  /** Escape closes the panel and returns focus to the trigger. */
  handleDocumentKeydown(e: Pick<KeyboardEvent, "key">): void {
    if (e.key === "Escape" && !this.panel.hidden) {
      // focus() on the trigger bubbles focusin → open(); suppress for that turn.
      this.suppressOpen = true;
      try {
        this.close();
        this.trigger.focus();
      } finally {
        this.suppressOpen = false;
      }
    }
  }

  mount(): void {
    this.root.addEventListener("mouseenter", this.onOpen);
    this.root.addEventListener("mouseleave", this.onScheduleClose);
    this.trigger.addEventListener("click", this.onTriggerClick);
    this.root.addEventListener("focusin", this.onOpen);
    this.root.addEventListener("focusout", this.onFocusOut);
    document.addEventListener("click", this.onDocumentClick);
    document.addEventListener("keydown", this.onDocumentKeydown);

    this.disposers.push(() => {
      this.root.removeEventListener("mouseenter", this.onOpen);
      this.root.removeEventListener("mouseleave", this.onScheduleClose);
      this.trigger.removeEventListener("click", this.onTriggerClick);
      this.root.removeEventListener("focusin", this.onOpen);
      this.root.removeEventListener("focusout", this.onFocusOut);
      document.removeEventListener("click", this.onDocumentClick);
      document.removeEventListener("keydown", this.onDocumentKeydown);
    });
  }

  open(): void {
    if (this.suppressOpen) return;
    this.clearCloseTimer();
    this.panel.hidden = false;
    this.trigger.setAttribute("aria-expanded", "true");
    this.root.classList.add("is-open");
  }

  close(): void {
    this.clearCloseTimer();
    this.panel.hidden = true;
    this.trigger.setAttribute("aria-expanded", "false");
    this.root.classList.remove("is-open");
  }

  scheduleClose(): void {
    this.clearCloseTimer();
    this.closeTimer = this.setTimeoutFn(() => {
      this.closeTimer = null;
      this.close();
    }, this.closeDelayMs);
  }

  isOpen(): boolean {
    return !this.panel.hidden;
  }

  dispose(): void {
    this.clearCloseTimer();
    while (this.disposers.length) {
      this.disposers.pop()?.();
    }
  }

  private clearCloseTimer(): void {
    if (this.closeTimer !== null) {
      this.clearTimeoutFn(this.closeTimer);
      this.closeTimer = null;
    }
  }
}
