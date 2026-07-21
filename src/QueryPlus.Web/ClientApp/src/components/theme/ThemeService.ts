import { inject, injectable, singleton } from "tsyringe";
import { TOKENS } from "../../core/di/tokens";
import {
  applyThemePreference,
  readThemePreference,
  systemPrefersDark,
  type ThemePreference,
  writeThemePreference,
} from "./theme";

const SELECTOR = "[data-theme-select]";
const ICON_SELECTOR = "[data-theme-icon]";

/**
 * Header theme control: Light / Dark / System.
 * Persists preference in localStorage; default is System (OS preference).
 */
@singleton()
@injectable()
export class ThemeService {
  private unsub: (() => void) | null = null;
  private media: MediaQueryList | null = null;

  constructor(
    @inject(TOKENS.Document) private readonly doc: Document,
    @inject(TOKENS.Window) private readonly win: Window,
  ) {}

  mount(root: ParentNode = this.doc): void {
    this.dispose();

    const select = root.querySelector<HTMLSelectElement>(SELECTOR);
    const preference = readThemePreference(this.win.localStorage);
    this.apply(preference);
    if (select) {
      select.value = preference;
    }
    this.syncIcon(preference);

    const onChange = (event: Event) => {
      const el = event.target;
      if (!(el instanceof HTMLSelectElement) || !el.matches(SELECTOR)) return;
      const next = el.value as ThemePreference;
      if (next !== "light" && next !== "dark" && next !== "system") return;
      writeThemePreference(this.win.localStorage, next);
      this.apply(next);
      this.syncIcon(next);
    };

    // Capture on document so we work even if select is re-rendered.
    this.doc.addEventListener("change", onChange);

    try {
      this.media = this.win.matchMedia("(prefers-color-scheme: dark)");
      const onMedia = () => {
        const pref = readThemePreference(this.win.localStorage);
        if (pref === "system") {
          this.apply("system");
        }
      };
      this.media.addEventListener("change", onMedia);
      this.unsub = () => {
        this.doc.removeEventListener("change", onChange);
        this.media?.removeEventListener("change", onMedia);
        this.media = null;
      };
    } catch {
      this.unsub = () => this.doc.removeEventListener("change", onChange);
    }
  }

  dispose(): void {
    this.unsub?.();
    this.unsub = null;
  }

  private apply(preference: ThemePreference): void {
    applyThemePreference(this.doc.documentElement, preference, systemPrefersDark(this.win));
  }

  private syncIcon(preference: ThemePreference): void {
    const icon = this.doc.querySelector(ICON_SELECTOR);
    if (!icon) return;
    icon.classList.remove("fa-sun", "fa-moon", "fa-circle-half-stroke", "fa-desktop");
    if (preference === "light") {
      icon.classList.add("fa-sun");
    } else if (preference === "dark") {
      icon.classList.add("fa-moon");
    } else {
      icon.classList.add("fa-circle-half-stroke");
    }
  }
}
