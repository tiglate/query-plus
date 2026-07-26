import { useVirtualizer } from "@tanstack/react-virtual";
import { ArrowDown, ArrowUp, ChevronsUpDown, GripVertical } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import type { GridCell, GridColumn } from "@/api/types";
import { cn } from "@/lib/utils";

interface ResultsGridProps {
    columns: GridColumn[];
    rows: GridCell[][];
    meta?: string;
}

const MIN_COLUMN_WIDTH = 80;
const MAX_COLUMN_WIDTH = 480;
const CELL_PADDING_PX = 16;
const SAMPLE_SIZE = 200;
const APPROX_CHAR_WIDTH = 7.5;

function compare(a: GridCell, b: GridCell): number {
    if (a === b) return 0;
    if (a === null) return -1;
    if (b === null) return 1;
    if (typeof a === "number" && typeof b === "number") return a - b;
    return String(a).localeCompare(String(b), undefined, { numeric: true, sensitivity: "base" });
}

function cellText(value: GridCell | undefined): string {
    if (value === null || value === undefined) return "";
    return String(value);
}

function approxWidth(text: string): number {
    return text.length * APPROX_CHAR_WIDTH;
}

function clamp(value: number): number {
    return Math.max(MIN_COLUMN_WIDTH, Math.min(MAX_COLUMN_WIDTH, value));
}

function currentRowHeight(): number {
    const size = Number.parseFloat(getComputedStyle(document.documentElement).fontSize);
    return Math.round((Number.isFinite(size) ? size : 17) * 2);
}

export function ResultsGrid({ columns: sourceColumns, rows, meta }: Readonly<ResultsGridProps>) {
    const visible = useMemo(
        () =>
            sourceColumns
                .map((column, sourceIndex) => ({ column, sourceIndex }))
                .filter((x) => x.column.visible),
        [sourceColumns],
    );
    const [order, setOrder] = useState<number[]>([]);
    const [userWidths, setUserWidths] = useState<Record<number, number>>({});
    const userSizedRef = useRef<Set<number>>(new Set());
    const [autoWidths, setAutoWidths] = useState<Record<number, number>>({});
    const [sort, setSort] = useState<{ index: number; asc: boolean } | null>(null);
    const parentRef = useRef<HTMLDivElement>(null);
    const measurerHeaderRef = useRef<HTMLDivElement>(null);
    const measurerBodyRef = useRef<HTMLSpanElement>(null);
    const dragRef = useRef<number | null>(null);
    const rowHeightRef = useRef(currentRowHeight());
    const [rowHeight, setRowHeight] = useState(rowHeightRef.current);
    const effectiveOrder =
        order.length === visible.length ? order : visible.map((_, index) => index);
    const ordered = effectiveOrder
        .map((index) => visible[index])
        .filter((value) => value !== undefined);
    const sortedRows = useMemo(() => {
        if (!sort) return rows;
        return [...rows].sort(
            (a, b) => compare(a[sort.index] ?? null, b[sort.index] ?? null) * (sort.asc ? 1 : -1),
        );
    }, [rows, sort]);
    const virtualizer = useVirtualizer({
        count: sortedRows.length,
        getScrollElement: () => parentRef.current,
        estimateSize: () => rowHeightRef.current,
        overscan: 12,
    });

    useEffect(() => {
        const handler = () => {
            rowHeightRef.current = currentRowHeight();
            setRowHeight(rowHeightRef.current);
            virtualizer.measure();
        };
        window.addEventListener("qp-font-size-change", handler);
        return () => window.removeEventListener("qp-font-size-change", handler);
    }, [virtualizer]);

    useEffect(() => {
        userSizedRef.current = new Set();
        setUserWidths({});
    }, [sourceColumns]);
    useEffect(() => {
        if (visible.length === 0) return;
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
            for (const element of visible) {
                if (cancelled) return;
                const entry = element;
                if (!entry) continue;
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
                next[sourceIndex] = clamp(Math.max(headerWidth, maxBodyWidth + CELL_PADDING_PX));
            }
            if (cancelled) return;
            setAutoWidths((current) => {
                let same = true;
                for (const { sourceIndex } of visible) {
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
        // eslint-disable-next-line react-hooks/exhaustive-deps -- measurement depends only on column shape; pagination must not retrigger expensive DOM reads
    }, [visible]);

    const columnWidth = (sourceIndex: number): number => {
        if (userSizedRef.current.has(sourceIndex)) {
            return userWidths[sourceIndex] ?? MIN_COLUMN_WIDTH;
        }
        return autoWidths[sourceIndex] ?? MIN_COLUMN_WIDTH;
    };

    const template = ordered.map((item) => `${columnWidth(item.sourceIndex)}px`).join(" ");

    const resize = (sourceIndex: number, startX: number, startWidth: number) => {
        const move = (event: MouseEvent) => {
            const width = clamp(startWidth + event.clientX - startX);
            setUserWidths((current) => ({ ...current, [sourceIndex]: width }));
        };
        const up = () => {
            document.removeEventListener("mousemove", move);
            document.removeEventListener("mouseup", up);
        };
        document.addEventListener("mousemove", move);
        document.addEventListener("mouseup", up);
    };

    return (
        <div className="flex min-h-0 flex-1 flex-col overflow-hidden">
            {meta && (
                <div className="border-b border-slate-200 px-3 py-1.5 text-small-label text-slate-500 dark:border-navy-600">
                    {meta}
                </div>
            )}
            <div
                ref={measurerHeaderRef}
                aria-hidden
                className="pointer-events-none fixed left-[-9999px] top-[-9999px] flex items-center gap-1 whitespace-nowrap border-r border-slate-200 bg-slate-50 px-2 text-small-label font-semibold"
            >
                <GripVertical className="h-3 w-3 shrink-0" />
                <span data-measurer-text />
                <ChevronsUpDown className="h-3 w-3 shrink-0 opacity-40" />
            </div>
            <span
                ref={measurerBodyRef}
                aria-hidden
                className="pointer-events-none fixed left-[-9999px] top-[-9999px] whitespace-nowrap text-dense"
            />
            <div ref={parentRef} className="min-h-0 flex-1 overflow-auto">
                <table
                    className="block text-dense"
                    style={{ width: "max-content", minWidth: "100%" }}
                >
                    <colgroup>
                        {ordered.map(({ sourceIndex }) => (
                            <col
                                key={sourceIndex}
                                style={{ width: `${columnWidth(sourceIndex)}px` }}
                            />
                        ))}
                    </colgroup>
                    <thead className="sticky top-0 z-20 block bg-slate-50 dark:bg-navy-900">
                        <tr className="grid" style={{ gridTemplateColumns: template }}>
                            {ordered.map(({ column, sourceIndex }, displayIndex) => {
                                const headerText = column.caption || column.technicalName;
                                const isSorted = sort?.index === sourceIndex;
                                return (
                                    <th
                                        key={`${sourceIndex}-${column.technicalName}`}
                                        scope="col"
                                        draggable
                                        onDragStart={() => {
                                            dragRef.current = displayIndex;
                                        }}
                                        onDragOver={(event) => event.preventDefault()}
                                        onDrop={() => {
                                            const from = dragRef.current;
                                            dragRef.current = null;
                                            if (from === null || from === displayIndex) return;
                                            setOrder((current) => {
                                                const next =
                                                    current.length === visible.length
                                                        ? [...current]
                                                        : visible.map((_, index) => index);
                                                const [moved] = next.splice(from, 1);
                                                if (moved !== undefined)
                                                    next.splice(displayIndex, 0, moved);
                                                return next;
                                            });
                                        }}
                                        className="group relative h-9 border-b border-r border-slate-200 bg-slate-50 px-2 text-left text-small-label font-semibold dark:border-navy-600 dark:bg-navy-900"
                                    >
                                        <button
                                            type="button"
                                            className="flex h-full w-full items-center gap-1 overflow-hidden bg-transparent text-left"
                                            onClick={() =>
                                                setSort((current) => ({
                                                    index: sourceIndex,
                                                    asc:
                                                        current?.index === sourceIndex
                                                            ? !current.asc
                                                            : true,
                                                }))
                                            }
                                        >
                                            <GripVertical className="h-3 w-3 shrink-0 opacity-30" />
                                            <span className="truncate" title={headerText}>
                                                {headerText}
                                            </span>
                                            {isSorted ? (
                                                sort?.asc ? (
                                                    <ArrowUp className="h-3 w-3 shrink-0" />
                                                ) : (
                                                    <ArrowDown className="h-3 w-3 shrink-0" />
                                                )
                                            ) : (
                                                <ChevronsUpDown className="h-3 w-3 shrink-0 opacity-40" />
                                            )}
                                        </button>
                                        <span
                                            className="absolute right-0 top-0 z-20 h-full w-1 cursor-col-resize bg-cyan-500 opacity-0 group-hover:opacity-100"
                                            onMouseDown={(event) => {
                                                event.preventDefault();
                                                event.stopPropagation();
                                                userSizedRef.current.add(sourceIndex);
                                                resize(
                                                    sourceIndex,
                                                    event.clientX,
                                                    columnWidth(sourceIndex),
                                                );
                                            }}
                                            onClick={(event) => event.stopPropagation()}
                                        />
                                    </th>
                                );
                            })}
                        </tr>
                    </thead>
                    <tbody
                        className="block"
                        style={{
                            position: "relative",
                            height: `${virtualizer.getTotalSize()}px`,
                        }}
                    >
                        {virtualizer.getVirtualItems().map((item) => {
                            const row = sortedRows[item.index] ?? [];
                            return (
                                <tr
                                    key={item.key}
                                    className={cn(
                                        "absolute left-0 top-0 grid w-full",
                                        item.index % 2 && "bg-slate-50/60 dark:bg-navy-900/30",
                                    )}
                                    style={{
                                        gridTemplateColumns: template,
                                        transform: `translateY(${item.start}px)`,
                                        height: `${rowHeight}px`,
                                    }}
                                >
                                    {ordered.map(({ column, sourceIndex }) => (
                                        <td
                                            key={sourceIndex}
                                            title={String(row[sourceIndex] ?? "")}
                                            className={cn(
                                                "truncate border-r border-slate-100 px-2 py-2 align-middle dark:border-navy-700",
                                                column.alignment === 1 && "text-center",
                                                column.alignment === 2 && "text-right tabular-nums",
                                            )}
                                        >
                                            {String(row[sourceIndex] ?? "")}
                                        </td>
                                    ))}
                                </tr>
                            );
                        })}
                    </tbody>
                </table>
            </div>
        </div>
    );
}
