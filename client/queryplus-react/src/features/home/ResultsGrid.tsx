import { useVirtualizer } from "@tanstack/react-virtual";
import { ArrowDown, ArrowUp, ChevronsUpDown, GripVertical } from "lucide-react";
import { memo, useEffect, useMemo, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import type { GridCell, GridColumn } from "@/api/types";
import { cn } from "@/lib/utils";
import { formatGridRows } from "./utils/grid-format";
import { sortRows, type SortState } from "./utils/grid-sort";
import { useColumnWidths, type VisibleColumn } from "./hooks/useColumnWidths";

interface ResultsGridProps {
    columns: GridColumn[];
    rows: GridCell[][];
    meta?: string;
}

function currentRowHeight(): number {
    const size = Number.parseFloat(getComputedStyle(document.documentElement).fontSize);
    // Must stay in sync with the header <th> height (h-9 = 2.25rem).
    return Math.round((Number.isFinite(size) ? size : 17) * 2.25);
}

function ResultsGridImpl({ columns: sourceColumns, rows, meta }: Readonly<ResultsGridProps>) {
    const { i18n } = useTranslation();
    const visible: VisibleColumn[] = useMemo(
        () =>
            sourceColumns
                .map((column, sourceIndex) => ({ column, sourceIndex }))
                .filter((x) => x.column.visible),
        [sourceColumns],
    );

    const [order, setOrder] = useState<number[]>([]);
    const [sort, setSort] = useState<SortState | null>(null);

    const parentRef = useRef<HTMLDivElement>(null);
    const measurerHeaderRef = useRef<HTMLDivElement>(null);
    const measurerBodyRef = useRef<HTMLSpanElement>(null);
    const dragRef = useRef<number | null>(null);
    const rowHeightRef = useRef(currentRowHeight());
    const [rowHeight, setRowHeight] = useState(rowHeightRef.current);

    const ordered = useMemo(() => {
        const effectiveOrder =
            order.length === visible.length ? order : visible.map((_, index) => index);
        return effectiveOrder
            .map((index) => visible[index])
            .filter((value): value is VisibleColumn => value !== undefined);
    }, [order, visible]);

    // Sort on the raw values first (ISO date strings sort correctly as plain strings; locale
    // date-formatting them first would break that), then format only for display/measurement.
    const sortedRows = useMemo(() => sortRows(rows, sort), [rows, sort]);
    const displayRows = useMemo(
        () => formatGridRows(sortedRows, i18n.language),
        [sortedRows, i18n.language],
    );

    const { getColumnWidth, handleResizeStart } = useColumnWidths(
        visible,
        displayRows,
        measurerHeaderRef,
        measurerBodyRef,
    );

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

    const template = ordered.map((item) => `${getColumnWidth(item.sourceIndex)}px`).join(" ");

    return (
        <div className="flex min-h-0 flex-1 flex-col overflow-hidden">
            {meta && (
                <div className="border-b border-slate-200 px-3 py-1.5 text-small-label text-muted dark:border-navy-600">
                    {meta}
                </div>
            )}
            <div
                ref={measurerHeaderRef}
                aria-hidden
                className="pointer-events-none fixed left-[-9999px] top-[-9999px] flex items-center gap-1 whitespace-nowrap border-r border-slate-200 bg-slate-100 px-3 text-small-label font-semibold"
            >
                <GripVertical className="h-3 w-3 shrink-0" />
                <span data-measurer-text />
                <ChevronsUpDown className="h-3 w-3 shrink-0 opacity-40" />
            </div>
            <span
                ref={measurerBodyRef}
                aria-hidden
                className="pointer-events-none fixed left-[-9999px] top-[-9999px] whitespace-nowrap border-r border-slate-100 px-3 text-dense"
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
                                style={{ width: `${getColumnWidth(sourceIndex)}px` }}
                            />
                        ))}
                    </colgroup>
                    <thead className="sticky top-0 z-20 block bg-slate-100 dark:bg-navy-900">
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
                                        className="group relative h-9 border-b border-r border-slate-200 bg-slate-100 px-3 text-left text-small-label font-semibold dark:border-navy-600 dark:bg-navy-900"
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
                                                handleResizeStart(
                                                    sourceIndex,
                                                    event.clientX,
                                                    getColumnWidth(sourceIndex),
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
                            const row = displayRows[item.index] ?? [];
                            return (
                                <tr
                                    key={item.key}
                                    className={cn(
                                        "absolute left-0 top-0 grid w-full hover:bg-cyan-50 dark:hover:bg-navy-700",
                                        item.index % 2 && "bg-surface-muted",
                                    )}
                                    style={{
                                        gridTemplateColumns: template,
                                        transform: `translateY(${item.start}px)`,
                                        height: `${rowHeight}px`,
                                    }}
                                >
                                    {ordered.map(({ column, sourceIndex }) => {
                                        const text = String(row[sourceIndex] ?? "");
                                        return (
                                            <td
                                                key={sourceIndex}
                                                title={text}
                                                className={cn(
                                                    "truncate border-r border-slate-100 px-3 py-2 align-middle dark:border-navy-700",
                                                    column.alignment === 1 && "text-center",
                                                    column.alignment === 2 &&
                                                        "text-right tabular-nums",
                                                )}
                                            >
                                                {text}
                                            </td>
                                        );
                                    })}
                                </tr>
                            );
                        })}
                    </tbody>
                </table>
            </div>
        </div>
    );
}

export const ResultsGrid = memo(ResultsGridImpl);
