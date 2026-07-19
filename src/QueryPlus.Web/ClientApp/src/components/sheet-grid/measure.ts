import { COL_MAX, COL_MIN, COL_PAD, WIDTH_SAMPLE } from "./constants";
import type { SheetColumn } from "./types";

let measureCanvasCtx: CanvasRenderingContext2D | null = null;

export function getMeasureContext(
  root: ParentNode | null | undefined,
  win: Window = window,
): CanvasRenderingContext2D | null {
  if (!measureCanvasCtx) {
    try {
      const canvas = win.document.createElement("canvas");
      measureCanvasCtx = canvas.getContext("2d");
    } catch {
      // jsdom without canvas package throws "Not implemented".
      measureCanvasCtx = null;
    }
  }
  if (!measureCanvasCtx) return null;

  const sample =
    (root instanceof Element
      ? root.querySelector(".qp-sheet-th") || root.querySelector(".qp-sheet")
      : null) || win.document.body;
  try {
    const style = win.getComputedStyle(sample as Element);
    measureCanvasCtx.font = `${style.fontWeight || "600"} ${style.fontSize || "12px"} ${
      style.fontFamily || "Inter, Segoe UI, system-ui, sans-serif"
    }`;
  } catch {
    // ignore style probe failures in tests
  }
  return measureCanvasCtx;
}

/** Exported for tests — reset shared canvas context between cases. */
export function resetMeasureContextForTests(): void {
  measureCanvasCtx = null;
}

export function measureTextWidth(
  ctx: CanvasRenderingContext2D | null,
  text: string,
): number {
  if (!ctx || !text) return 0;
  const sample = text.length > 80 ? text.slice(0, 80) + "…" : text;
  // Strip simple HTML for sizing action columns roughly.
  const plain = sample.replace(/<[^>]+>/g, " ");
  return ctx.measureText(plain).width;
}

export function autoSizeColumns(
  columns: SheetColumn[],
  cells: string[][],
  root: ParentNode | null | undefined,
  win: Window = window,
): void {
  const ctx = getMeasureContext(root, win);
  const sample = Math.min(WIDTH_SAMPLE, cells.length);

  for (let c = 0; c < columns.length; c++) {
    if (columns[c].width && columns[c].fixedWidth) continue;

    let maxPx = measureTextWidth(ctx, columns[c].caption || "");
    if (columns[c].html) {
      maxPx = Math.max(maxPx, columns[c].width || 180);
    } else {
      for (let r = 0; r < sample; r++) {
        const w = measureTextWidth(ctx, cells[r]?.[c] || "");
        if (w > maxPx) maxPx = w;
      }
    }
    columns[c].width = Math.round(
      Math.min(COL_MAX, Math.max(COL_MIN, maxPx + COL_PAD)),
    );
  }
}

export function totalColumnsWidth(columns: SheetColumn[]): number {
  return columns.reduce((sum, col) => sum + (col.width || COL_MIN), 0);
}
