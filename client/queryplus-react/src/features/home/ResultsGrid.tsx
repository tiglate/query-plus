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

function compare(a: GridCell, b: GridCell): number {
    if (a === b) return 0;
    if (a === null) return -1;
    if (b === null) return 1;
    if (typeof a === "number" && typeof b === "number") return a - b;
    return String(a).localeCompare(String(b), undefined, { numeric: true, sensitivity: "base" });
}

function currentRowHeight(): number {
    const size = parseFloat(getComputedStyle(document.documentElement).fontSize);
    return Math.round((Number.isFinite(size) ? size : 17) * 2);
}

export function ResultsGrid({ columns: sourceColumns, rows, meta }: ResultsGridProps) {
    const visible = useMemo(
        () =>
            sourceColumns
                .map((column, sourceIndex) => ({ column, sourceIndex }))
                .filter((x) => x.column.visible),
        [sourceColumns],
    );
    const [order, setOrder] = useState<number[]>([]);
    const [widths, setWidths] = useState<Record<number, number>>({});
    const [sort, setSort] = useState<{ index: number; asc: boolean } | null>(null);
    const parentRef = useRef<HTMLDivElement>(null);
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
    const template = ordered
        .map(
            (item) =>
                `${widths[item.sourceIndex] ?? Math.max(120, Math.min(320, item.column.caption.length * 11 + 60))}px`,
        )
        .join(" ");

    const resize = (sourceIndex: number, startX: number, startWidth: number) => {
        const move = (event: MouseEvent) =>
            setWidths((current) => ({
                ...current,
                [sourceIndex]: Math.max(48, Math.min(480, startWidth + event.clientX - startX)),
            }));
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
            <div className="overflow-x-auto border-b border-slate-200 bg-slate-50 dark:border-navy-600 dark:bg-navy-900">
                <div
                    className="grid min-w-max"
                    style={{ gridTemplateColumns: template }}
                    role="row"
                >
                    {ordered.map(({ column, sourceIndex }, displayIndex) => (
                        <button
                            type="button"
                            role="columnheader"
                            draggable
                            key={`${sourceIndex}-${column.technicalName}`}
                            className="group relative flex h-9 select-none items-center gap-1 overflow-hidden border-r border-slate-200 px-2 text-left text-small-label font-semibold dark:border-navy-600"
                            onClick={() =>
                                setSort((current) => ({
                                    index: sourceIndex,
                                    asc: current?.index === sourceIndex ? !current.asc : true,
                                }))
                            }
                            onDragStart={() => {
                                dragRef.current = displayIndex;
                            }}
                            onDragOver={(event) => event.preventDefault()}
                            onDrop={() => {
                                const from = dragRef.current;
                                if (from === null || from === displayIndex) return;
                                setOrder((current) => {
                                    const next =
                                        current.length === visible.length
                                            ? [...current]
                                            : visible.map((_, index) => index);
                                    const [moved] = next.splice(from, 1);
                                    if (moved !== undefined) next.splice(displayIndex, 0, moved);
                                    return next;
                                });
                                dragRef.current = null;
                            }}
                        >
                            <GripVertical className="h-3 w-3 shrink-0 opacity-30" />
                            <span className="truncate">
                                {column.caption || column.technicalName}
                            </span>
                            {sort?.index === sourceIndex ? (
                                sort.asc ? (
                                    <ArrowUp className="h-3 w-3" />
                                ) : (
                                    <ArrowDown className="h-3 w-3" />
                                )
                            ) : (
                                <ChevronsUpDown className="h-3 w-3 opacity-40" />
                            )}
                            <span
                                className="absolute right-0 top-0 h-full w-1 cursor-col-resize bg-cyan-500 opacity-0 group-hover:opacity-100"
                                onMouseDown={(event) => {
                                    event.preventDefault();
                                    event.stopPropagation();
                                    resize(sourceIndex, event.clientX, widths[sourceIndex] ?? 160);
                                }}
                            />
                        </button>
                    ))}
                </div>
            </div>
            <div ref={parentRef} className="min-h-0 flex-1 overflow-auto" role="table">
                <div className="relative min-w-max" style={{ height: virtualizer.getTotalSize() }}>
                    {virtualizer.getVirtualItems().map((item) => {
                        const row = sortedRows[item.index] ?? [];
                        return (
                            <div
                                role="row"
                                key={item.key}
                                className={cn(
                                    "absolute left-0 top-0 grid min-w-max border-b border-slate-100 text-dense dark:border-navy-700",
                                    item.index % 2 && "bg-slate-50/60 dark:bg-navy-900/30",
                                )}
                                style={{
                                    transform: `translateY(${item.start}px)`,
                                    gridTemplateColumns: template,
                                    height: `${rowHeight}px`,
                                }}
                            >
                                {ordered.map(({ column, sourceIndex }) => (
                                    <div
                                        role="cell"
                                        key={sourceIndex}
                                        title={String(row[sourceIndex] ?? "")}
                                        className={cn(
                                            "truncate border-r border-slate-100 px-2 py-2 dark:border-navy-700",
                                            column.alignment === 1 && "text-center",
                                            column.alignment === 2 && "text-right tabular-nums",
                                        )}
                                    >
                                        {String(row[sourceIndex] ?? "")}
                                    </div>
                                ))}
                            </div>
                        );
                    })}
                </div>
            </div>
        </div>
    );
}
