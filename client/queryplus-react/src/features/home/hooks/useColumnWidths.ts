import { useEffect, useRef, useState, type RefObject } from "react";
import type { GridCell, GridColumn } from "@/api/types";

export const MIN_COLUMN_WIDTH = 80;
export const MAX_COLUMN_WIDTH = 480;
export const SAMPLE_SIZE = 200;
export const APPROX_CHAR_WIDTH = 7.5;
// `offsetWidth` (used for the header measurement) rounds a subpixel-precise natural width
// (e.g. 117.05px) down to an integer (117px), which then gets applied as an exact CSS width
// with zero slack - any subpixel rendering difference between the offscreen measurer and the
// real scrollable table (a different layout context) is then enough to trip text-overflow:
// ellipsis. A couple of extra pixels of headroom absorbs that without being visually
// noticeable in an "auto-fit" column.
export const MEASUREMENT_SAFETY_MARGIN_PX = 2;
// Fallback root font-size (px) used only when the live root can't be read (e.g. jsdom tests).
// Mirrors ResultsGrid.tsx's currentRowHeight() fallback, which uses the same default.
export const DEFAULT_ROOT_FONT_SIZE_PX = 17;

export function cellText(value: GridCell | undefined): string {
    if (value === null || value === undefined) return "";
    return String(value);
}

export function approxWidth(text: string): number {
    return text.length * APPROX_CHAR_WIDTH;
}

export function clampWidth(value: number): number {
    return Math.max(MIN_COLUMN_WIDTH, Math.min(MAX_COLUMN_WIDTH, value));
}

// Fallback horizontal "box overhead" (padding + border) estimate, used only when there's no
// live measurer node to read the real value from (e.g. jsdom tests). Mirrors the <td>'s
// px-3 (0.75rem each side) padding at the default root font-size; deliberately doesn't try to
// guess the border width too since it's only ever a rough approximation anyway.
export function cellPaddingPx(): number {
    const rootPx = Number.parseFloat(getComputedStyle(document.documentElement).fontSize);
    return 2 * 0.75 * (Number.isFinite(rootPx) ? rootPx : DEFAULT_ROOT_FONT_SIZE_PX);
}

// Real horizontal padding + border width of a live element, read directly from its computed
// style rather than assumed - the <td> this mirrors has both a px-3 padding AND a 1px
// border-r, and hand-deriving "0.75rem of padding" alone under-counted the border, leaving
// just enough of a shortfall to clip only the widest string in a column while shorter ones fit.
function boxOverheadPx(style: CSSStyleDeclaration): number {
    const overhead =
        Number.parseFloat(style.paddingLeft) +
        Number.parseFloat(style.paddingRight) +
        Number.parseFloat(style.borderLeftWidth) +
        Number.parseFloat(style.borderRightWidth);
    return Number.isFinite(overhead) ? overhead : cellPaddingPx();
}

let measureCanvas: HTMLCanvasElement | null = null;
function getMeasureContext(): CanvasRenderingContext2D | null {
    measureCanvas ??= document.createElement("canvas");
    return measureCanvas.getContext("2d");
}

export interface WidestText {
    text: string;
    width: number;
}

// Widest-rendering string (and its pixel width) across every string in `texts`, via canvas
// text metrics (no DOM layout reflow). Checking every sampled row's real width - not just a
// single "longest by character count" guess - matters because a short string can render wider
// than a longer one in a proportional font (e.g. a handful of capital letters vs. more
// numerals), which was causing short cell text to get truncated when the wrong row was picked
// as the reference. Returning the winning *text*, not just its width, lets the caller cross-
// check that exact string against the DOM - canvas and DOM text layout can disagree by a
// pixel or two even for same-length strings (kerning), so two equal-length values can measure
// differently; only re-measuring the actual winner (not an arbitrary same-length pick) closes
// that gap.
export function widestText(ctx: CanvasRenderingContext2D | null, texts: string[]): WidestText {
    let text = "";
    let width = 0;
    for (const candidate of texts) {
        const candidateWidth = ctx ? ctx.measureText(candidate).width : approxWidth(candidate);
        if (candidateWidth > width) {
            text = candidate;
            width = candidateWidth;
        }
    }
    return { text, width };
}

export interface VisibleColumn {
    column: GridColumn;
    sourceIndex: number;
}

export function useColumnWidths(
    visibleColumns: VisibleColumn[],
    rows: GridCell[][],
    measurerHeaderRef: RefObject<HTMLDivElement | null>,
    measurerBodyRef: RefObject<HTMLSpanElement | null>,
) {
    const [userWidths, setUserWidths] = useState<Record<number, number>>({});
    const [autoWidths, setAutoWidths] = useState<Record<number, number>>({});
    const userSizedRef = useRef<Set<number>>(new Set());
    const [fontSizeTick, setFontSizeTick] = useState(0);

    useEffect(() => {
        userSizedRef.current = new Set();
        setUserWidths({});
    }, [visibleColumns.map((c) => c.sourceIndex).join(",")]);

    // Auto-computed widths are in real pixels, so they go stale when the root font-size
    // stepper changes (preferences.ts's changeFontSize) - re-measure on that event the same
    // way ResultsGrid.tsx's row height already does. Manually-resized (userSizedRef) columns
    // are left alone, matching the skip below in the measurement loop.
    useEffect(() => {
        const handler = () => setFontSizeTick((tick) => tick + 1);
        window.addEventListener("qp-font-size-change", handler);
        return () => window.removeEventListener("qp-font-size-change", handler);
    }, []);

    useEffect(() => {
        if (visibleColumns.length === 0) return;
        let cancelled = false;
        let raf = 0;
        const run = () => {
            if (cancelled) return;
            const headerNode = measurerHeaderRef.current;
            const bodyNode = measurerBodyRef.current;
            const headerTextSlot =
                headerNode?.querySelector<HTMLSpanElement>("[data-measurer-text]");

            const measureHeader = (text: string): number => {
                if (!headerNode || !headerTextSlot) return approxWidth(text);
                headerTextSlot.textContent = text;
                const width = headerNode.offsetWidth;
                return width > 0 ? width : approxWidth(text);
            };

            // Canvas-based, so every sampled row can be checked cheaply (no per-row DOM
            // layout reflow) instead of guessing a single "longest" row up front.
            const ctx = bodyNode ? getMeasureContext() : null;
            let overheadPx = cellPaddingPx();
            if (bodyNode) {
                const style = getComputedStyle(bodyNode);
                overheadPx = boxOverheadPx(style);
                if (ctx) ctx.font = `${style.fontWeight} ${style.fontSize} ${style.fontFamily}`;
            }

            const total = rows.length;
            const stride = total > SAMPLE_SIZE ? Math.floor(total / SAMPLE_SIZE) : 1;
            const next: Record<number, number> = {};
            const sizedColumns: number[] = [];

            for (const entry of visibleColumns) {
                if (cancelled) return;
                const { column, sourceIndex } = entry;
                if (userSizedRef.current.has(sourceIndex)) continue;
                sizedColumns.push(sourceIndex);

                const headerText = column.caption || column.technicalName;
                const headerWidth = measureHeader(headerText);

                const sampledTexts: string[] = [];
                for (let i = 0; i < total; i += stride) {
                    sampledTexts.push(cellText(rows[i]?.[sourceIndex]));
                }
                if (stride > 1 && total > 0) {
                    sampledTexts.push(cellText(rows[total - 1]?.[sourceIndex]));
                }

                const widest = widestText(ctx, sampledTexts);
                let cellWidth = widest.width + overheadPx;
                if (bodyNode) {
                    // Re-measure the exact winning string via the DOM (not an arbitrary
                    // same-length pick) - canvas and DOM text layout can disagree by a pixel
                    // or two, and this also gives a genuine offsetWidth fallback for
                    // environments without canvas 2D support. bodyNode now carries the same
                    // padding/border classes as the real <td>, so its offsetWidth is already
                    // a full box width - directly comparable to widest.width + overheadPx.
                    bodyNode.textContent = widest.text;
                    const domWidth = bodyNode.offsetWidth;
                    if (domWidth > cellWidth) cellWidth = domWidth;
                }
                next[sourceIndex] = clampWidth(
                    Math.max(headerWidth, cellWidth) + MEASUREMENT_SAFETY_MARGIN_PX,
                );
            }

            if (cancelled) return;
            setAutoWidths((current) => {
                let same = true;
                for (const sourceIndex of sizedColumns) {
                    if ((current[sourceIndex] ?? -1) !== next[sourceIndex]) {
                        same = false;
                        break;
                    }
                }
                return same ? current : next;
            });
        };

        raf = requestAnimationFrame(run);
        return () => {
            cancelled = true;
            if (raf) cancelAnimationFrame(raf);
        };
    }, [visibleColumns, rows, fontSizeTick]);

    const getColumnWidth = (sourceIndex: number): number => {
        if (userSizedRef.current.has(sourceIndex)) {
            return userWidths[sourceIndex] ?? MIN_COLUMN_WIDTH;
        }
        return autoWidths[sourceIndex] ?? MIN_COLUMN_WIDTH;
    };

    const handleResizeStart = (sourceIndex: number, startX: number, startWidth: number) => {
        userSizedRef.current.add(sourceIndex);
        let raf = 0;
        let latestClientX = startX;
        const applyWidth = () => {
            raf = 0;
            const width = clampWidth(startWidth + latestClientX - startX);
            setUserWidths((current) => ({ ...current, [sourceIndex]: width }));
        };
        const move = (event: MouseEvent) => {
            latestClientX = event.clientX;
            // Coalesce to at most one setState per animation frame instead of one per
            // mousemove (which can fire well over 60/sec and re-renders the whole grid).
            if (!raf) raf = requestAnimationFrame(applyWidth);
        };
        const up = () => {
            // Flush any pending frame so the final width matches the cursor's last
            // position instead of whatever the last-rendered frame happened to catch.
            if (raf) {
                cancelAnimationFrame(raf);
                applyWidth();
            }
            document.removeEventListener("mousemove", move);
            document.removeEventListener("mouseup", up);
        };
        document.addEventListener("mousemove", move);
        document.addEventListener("mouseup", up);
    };

    return {
        getColumnWidth,
        handleResizeStart,
    };
}
