import { useEffect, useRef, useState, type RefObject } from "react";
import type { GridCell, GridColumn } from "@/api/types";

export const MIN_COLUMN_WIDTH = 80;
export const MAX_COLUMN_WIDTH = 480;
export const CELL_PADDING_PX = 16;
export const SAMPLE_SIZE = 200;
export const APPROX_CHAR_WIDTH = 7.5;

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

    useEffect(() => {
        userSizedRef.current = new Set();
        setUserWidths({});
    }, [visibleColumns.map((c) => c.sourceIndex).join(",")]);

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

            const measureBody = (text: string): number => {
                if (!bodyNode) return approxWidth(text);
                bodyNode.textContent = text;
                const width = bodyNode.offsetWidth;
                return width > 0 ? width : approxWidth(text);
            };

            const total = rows.length;
            const stride = total > SAMPLE_SIZE ? Math.floor(total / SAMPLE_SIZE) : 1;
            const bodyCeiling = MAX_COLUMN_WIDTH - CELL_PADDING_PX;
            const next: Record<number, number> = {};

            for (const entry of visibleColumns) {
                if (cancelled) return;
                const { column, sourceIndex } = entry;
                if (userSizedRef.current.has(sourceIndex)) continue;

                const headerText = column.caption || column.technicalName;
                const headerWidth = measureHeader(headerText);
                let maxBodyWidth = 0;

                if (total > 0) {
                    for (let i = 0; i < total; i += stride) {
                        const text = cellText(rows[i]?.[sourceIndex]);
                        const w = measureBody(text);
                        if (w > maxBodyWidth) {
                            maxBodyWidth = w;
                            if (maxBodyWidth >= bodyCeiling) break;
                        }
                    }
                    if (stride > 1) {
                        const lastText = cellText(rows[total - 1]?.[sourceIndex]);
                        const w = measureBody(lastText);
                        if (w > maxBodyWidth) maxBodyWidth = w;
                    }
                }
                next[sourceIndex] = clampWidth(
                    Math.max(headerWidth, maxBodyWidth + CELL_PADDING_PX),
                );
            }

            if (cancelled) return;
            setAutoWidths((current) => {
                let same = true;
                for (const { sourceIndex } of visibleColumns) {
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
    }, [visibleColumns, rows]);

    const getColumnWidth = (sourceIndex: number): number => {
        if (userSizedRef.current.has(sourceIndex)) {
            return userWidths[sourceIndex] ?? MIN_COLUMN_WIDTH;
        }
        return autoWidths[sourceIndex] ?? MIN_COLUMN_WIDTH;
    };

    const handleResizeStart = (sourceIndex: number, startX: number, startWidth: number) => {
        userSizedRef.current.add(sourceIndex);
        const move = (event: MouseEvent) => {
            const width = clampWidth(startWidth + event.clientX - startX);
            setUserWidths((current) => ({ ...current, [sourceIndex]: width }));
        };
        const up = () => {
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
