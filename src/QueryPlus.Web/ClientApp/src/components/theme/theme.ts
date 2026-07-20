/** User preference for color scheme. Default is system (OS). */
export type ThemePreference = "light" | "dark" | "system";

/** Resolved palette actually applied to the document. */
export type ResolvedTheme = "light" | "dark";

export const THEME_STORAGE_KEY = "qp-theme";

export function isThemePreference(value: unknown): value is ThemePreference {
  return value === "light" || value === "dark" || value === "system";
}

export function readThemePreference(
  storage: Pick<Storage, "getItem"> | null | undefined,
): ThemePreference {
  try {
    const raw = storage?.getItem(THEME_STORAGE_KEY);
    if (isThemePreference(raw)) return raw;
  } catch {
    // private mode / blocked storage
  }
  return "system";
}

export function writeThemePreference(
  storage: Pick<Storage, "setItem"> | null | undefined,
  preference: ThemePreference,
): void {
  try {
    storage?.setItem(THEME_STORAGE_KEY, preference);
  } catch {
    // private mode / blocked storage
  }
}

export function resolveTheme(
  preference: ThemePreference,
  prefersDark: boolean,
): ResolvedTheme {
  if (preference === "light") return "light";
  if (preference === "dark") return "dark";
  return prefersDark ? "dark" : "light";
}

/**
 * Apply light/dark to <html>: class `dark` + color-scheme for native controls.
 */
export function applyResolvedTheme(
  root: HTMLElement,
  resolved: ResolvedTheme,
): void {
  root.classList.toggle("dark", resolved === "dark");
  root.style.colorScheme = resolved;
  root.dataset.themeResolved = resolved;
}

export function applyThemePreference(
  root: HTMLElement,
  preference: ThemePreference,
  prefersDark: boolean,
): ResolvedTheme {
  root.dataset.theme = preference;
  const resolved = resolveTheme(preference, prefersDark);
  applyResolvedTheme(root, resolved);
  return resolved;
}

export function systemPrefersDark(
  win: Pick<Window, "matchMedia"> = window,
): boolean {
  try {
    return win.matchMedia("(prefers-color-scheme: dark)").matches;
  } catch {
    return false;
  }
}
