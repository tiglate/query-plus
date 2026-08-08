import { test, expect } from "vitest";
import { act, renderHook, waitFor } from "@testing-library/react";
import type { RefObject } from "react";
import {
    cellText,
    approxWidth,
    clampWidth,
    useColumnWidths,
    MIN_COLUMN_WIDTH,
    MAX_COLUMN_WIDTH,
    type VisibleColumn,
} from "./useColumnWidths";
import type { GridCell, GridColumn } from "@/api/types";

test("cellText converts null and undefined to empty string", () => {
    expect(cellText(null)).toBe("");
    expect(cellText(undefined)).toBe("");
    expect(cellText("hello")).toBe("hello");
    expect(cellText(123)).toBe("123");
});

test("approxWidth computes text length approximation", () => {
    expect(approxWidth("abc")).toBe(3 * 7.5);
});

test("clampWidth bounds value between min and max", () => {
    expect(clampWidth(10)).toBe(MIN_COLUMN_WIDTH);
    expect(clampWidth(1000)).toBe(MAX_COLUMN_WIDTH);
    expect(clampWidth(150)).toBe(150);
});

function makeRefs() {
    return {
        headerRef: { current: null } as RefObject<HTMLDivElement | null>,
        bodyRef: { current: null } as RefObject<HTMLSpanElement | null>,
    };
}

function makeColumn(technicalName: string, sourceIndex: number): VisibleColumn {
    const column: GridColumn = {
        technicalName,
        caption: technicalName,
        alignment: 0,
        formatMask: null,
        visible: true,
    };
    return { column, sourceIndex };
}

test("auto-sizes a column from the approximated header/body text width (jsdom reports 0 offsetWidth)", async () => {
    const { headerRef, bodyRef } = makeRefs();
    const columns = [makeColumn("Name", 0)];
    const rows: GridCell[][] = [["AliceWonderland"]]; // 15 chars, long enough to clear MIN_COLUMN_WIDTH
    const { result } = renderHook(() => useColumnWidths(columns, rows, headerRef, bodyRef));

    await waitFor(() => {
        expect(result.current.getColumnWidth(0)).toBeGreaterThan(MIN_COLUMN_WIDTH);
    });
    // approxWidth("AliceWonderland") + CELL_PADDING_PX = 15*7.5+16 = 128.5, clamped (no-op here)
    expect(result.current.getColumnWidth(0)).toBe(128.5);
});

test("a column with no rows falls back to MIN_COLUMN_WIDTH", async () => {
    const { headerRef, bodyRef } = makeRefs();
    const columns = [makeColumn("Id", 0)];
    const { result } = renderHook(() => useColumnWidths(columns, [], headerRef, bodyRef));

    await waitFor(() => {
        expect(result.current.getColumnWidth(0)).toBe(MIN_COLUMN_WIDTH);
    });
});

test("handleResizeStart marks the column as user-sized and tracks the drag to mouseup", async () => {
    const { headerRef, bodyRef } = makeRefs();
    const columns = [makeColumn("Name", 0)];
    const rows: GridCell[][] = [["AliceWonderland"]];
    const { result } = renderHook(() => useColumnWidths(columns, rows, headerRef, bodyRef));
    await waitFor(() => expect(result.current.getColumnWidth(0)).toBe(128.5));

    act(() => {
        result.current.handleResizeStart(0, 100, 150); // sourceIndex 0, drag starts at x=100 from a 150px width
    });
    act(() => {
        document.dispatchEvent(new MouseEvent("mousemove", { clientX: 140 })); // +40px
    });
    act(() => {
        document.dispatchEvent(new MouseEvent("mouseup")); // flushes the pending frame synchronously
    });

    expect(result.current.getColumnWidth(0)).toBe(clampWidth(190));
});

test("handleResizeStart clamps the dragged width to MAX_COLUMN_WIDTH", () => {
    const { headerRef, bodyRef } = makeRefs();
    const columns = [makeColumn("Name", 0)];
    const { result } = renderHook(() => useColumnWidths(columns, [], headerRef, bodyRef));

    act(() => {
        result.current.handleResizeStart(0, 0, 400);
    });
    act(() => {
        document.dispatchEvent(new MouseEvent("mousemove", { clientX: 1000 })); // way past MAX_COLUMN_WIDTH
    });
    act(() => {
        document.dispatchEvent(new MouseEvent("mouseup"));
    });

    expect(result.current.getColumnWidth(0)).toBe(MAX_COLUMN_WIDTH);
});

test("changing the visible-column set resets user-sized widths back to auto-sizing", async () => {
    const { headerRef, bodyRef } = makeRefs();
    const rows: GridCell[][] = [["AliceWonderland"]];
    const { result, rerender } = renderHook(
        ({ columns }: { columns: VisibleColumn[] }) => useColumnWidths(columns, rows, headerRef, bodyRef),
        { initialProps: { columns: [makeColumn("Name", 0)] } },
    );
    await waitFor(() => expect(result.current.getColumnWidth(0)).toBe(128.5));

    act(() => {
        result.current.handleResizeStart(0, 100, 150);
    });
    act(() => {
        document.dispatchEvent(new MouseEvent("mousemove", { clientX: 300 }));
    });
    act(() => {
        document.dispatchEvent(new MouseEvent("mouseup"));
    });
    expect(result.current.getColumnWidth(0)).toBe(clampWidth(350));

    // A different set of source indexes (e.g. the user toggled column visibility) drops the
    // manual override and falls back to auto-sizing again.
    rerender({ columns: [makeColumn("Name", 1)] });

    await waitFor(() => expect(result.current.getColumnWidth(0)).toBe(MIN_COLUMN_WIDTH));
});
