export function qs(root: ParentNode, ...selectors: string[]): HTMLElement | null {
    for (const sel of selectors) {
        const el = root.querySelector(sel);
        if (el instanceof HTMLElement) return el;
    }
    return null;
}

export function escapeHtml(text: unknown): string {
    return String(text ?? "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");
}
