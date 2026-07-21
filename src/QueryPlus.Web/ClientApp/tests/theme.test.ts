import { afterEach, beforeEach, describe, expect, it, vi } from "vite-plus/test";
import "reflect-metadata";
import { createTestContainer } from "@/core/di/container";
import { ThemeService } from "@/components/theme/ThemeService";
import {
  applyThemePreference,
  readThemePreference,
  resolveTheme,
  THEME_STORAGE_KEY,
  writeThemePreference,
} from "@/components/theme/theme";

describe("theme helpers", () => {
  it("resolves light/dark/system against OS preference", () => {
    expect(resolveTheme("light", true)).toBe("light");
    expect(resolveTheme("dark", false)).toBe("dark");
    expect(resolveTheme("system", true)).toBe("dark");
    expect(resolveTheme("system", false)).toBe("light");
  });

  it("reads and writes preference from storage", () => {
    const store = new Map<string, string>();
    const storage = {
      getItem: (k: string) => store.get(k) ?? null,
      setItem: (k: string, v: string) => {
        store.set(k, v);
      },
    };
    expect(readThemePreference(storage)).toBe("system");
    writeThemePreference(storage, "dark");
    expect(store.get(THEME_STORAGE_KEY)).toBe("dark");
    expect(readThemePreference(storage)).toBe("dark");
  });

  it("toggles dark class and color-scheme on root", () => {
    const root = document.documentElement;
    root.classList.remove("dark");
    applyThemePreference(root, "dark", false);
    expect(root.classList.contains("dark")).toBe(true);
    expect(root.style.colorScheme).toBe("dark");
    expect(root.dataset.theme).toBe("dark");
    applyThemePreference(root, "light", true);
    expect(root.classList.contains("dark")).toBe(false);
    expect(root.style.colorScheme).toBe("light");
  });
});

describe("ThemeService", () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove("dark");
    document.documentElement.removeAttribute("data-theme");
    document.body.innerHTML = `
      <i class="fa-solid fa-circle-half-stroke" data-theme-icon></i>
      <select data-theme-select>
        <option value="system">System</option>
        <option value="light">Light</option>
        <option value="dark">Dark</option>
      </select>
    `;
    // jsdom may not implement matchMedia
    Object.defineProperty(window, "matchMedia", {
      writable: true,
      configurable: true,
      value: (query: string): MediaQueryList =>
        ({
          matches: false,
          media: query,
          onchange: null,
          addListener: () => {},
          removeListener: () => {},
          addEventListener: () => {},
          removeEventListener: () => {},
          dispatchEvent: () => false,
        }) as MediaQueryList,
    });
  });

  afterEach(() => {
    document.body.innerHTML = "";
    document.documentElement.classList.remove("dark");
    localStorage.clear();
    vi.restoreAllMocks();
  });

  it("applies selection and persists preference", () => {
    const c = createTestContainer();
    const theme = c.resolve(ThemeService);
    theme.mount();

    const select = document.querySelector("[data-theme-select]") as HTMLSelectElement;
    select.value = "dark";
    select.dispatchEvent(new Event("change", { bubbles: true }));

    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe("dark");
    expect(document.documentElement.classList.contains("dark")).toBe(true);
    expect(document.querySelector("[data-theme-icon]")!.classList.contains("fa-moon")).toBe(true);

    select.value = "light";
    select.dispatchEvent(new Event("change", { bubbles: true }));
    expect(document.documentElement.classList.contains("dark")).toBe(false);
    expect(document.querySelector("[data-theme-icon]")!.classList.contains("fa-sun")).toBe(true);

    theme.dispose();
  });
});
