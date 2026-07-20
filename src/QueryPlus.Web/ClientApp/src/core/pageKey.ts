/**
 * Resolve page key from body[data-page] or any [data-page] with a non-empty value.
 *
 * Important: `querySelector("[data-page]")` matches `data-page=""` (empty).
 * Layout may render body[data-page] empty; the real key lives on content
 * (e.g. data-page="home"). Skip empty values.
 */
export function resolvePageKey(doc: Document = document): string {
  const keys: Array<string | null> = [
    doc.body?.getAttribute("data-page") ?? null,
  ];
  doc.querySelectorAll("[data-page]").forEach((el) => {
    keys.push(el.getAttribute("data-page"));
  });
  for (const raw of keys) {
    const key = (raw || "").trim();
    if (key) return key;
  }
  return "";
}
