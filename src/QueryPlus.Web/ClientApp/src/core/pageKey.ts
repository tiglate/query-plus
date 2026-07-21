/**
 * Resolve ClientApp page key.
 *
 * Priority:
 * 1. meta[name="qp-page"] (set in _Layout from ViewData — reliable for Razor)
 * 2. body[data-page] when non-empty
 * 3. first non-empty [data-page] in the document (e.g. content root)
 *
 * Empty strings are ignored so data-page="" never masks a real key.
 */
export function resolvePageKey(doc: Document = document): string {
    const keys: Array<string | null> = [
        doc.querySelector('meta[name="qp-page"]')?.getAttribute("content") ?? null,
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
