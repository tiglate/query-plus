import { ChevronLeft, ChevronRight } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";
import { cn, formatTemplate } from "@/lib/utils";

interface PagerProps {
    page: number;
    pageSize: number;
    total: number;
    onPage: (page: number) => void;
}

export function Pager({ page, pageSize, total, onPage }: Readonly<PagerProps>) {
    const { t } = useTranslation();
    const pages = Math.max(1, Math.ceil(total / pageSize));
    const start = Math.max(1, page - 2);
    const end = Math.min(pages, page + 2);
    const visible = Array.from({ length: end - start + 1 }, (_, index) => start + index);
    return (
        <footer className="flex flex-wrap items-center justify-between gap-2 border-t border-slate-200 px-3 py-2 dark:border-navy-600">
            <span className="text-small-label text-slate-600 dark:text-slate-300">
                {formatTemplate(
                    t("Pagination_Summary"),
                    (page - 1) * pageSize + 1,
                    Math.min(page * pageSize, total),
                    total,
                )}
            </span>
            {pages > 1 && (
                <div className="flex items-center gap-1">
                    <Button
                        variant="secondary"
                        size="sm"
                        disabled={page <= 1}
                        onClick={() => onPage(page - 1)}
                    >
                        <ChevronLeft className="h-3 w-3" /> {t("Pagination_Previous")}
                    </Button>
                    {start > 1 && (
                        <Button variant="ghost" size="sm" onClick={() => onPage(1)}>
                            1
                        </Button>
                    )}
                    {start > 2 && <span className="px-1 text-small-label">…</span>}
                    {visible.map((value) => (
                        <button
                            key={value}
                            type="button"
                            aria-current={value === page ? "page" : undefined}
                            onClick={() => onPage(value)}
                            className={cn(
                                "inline-flex h-8 items-center justify-center rounded-md px-3 text-dense font-semibold transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-cyan-500",
                                value === page
                                    ? "bg-lime-500 text-navy hover:bg-lime-600"
                                    : "text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-navy-700",
                            )}
                        >
                            {value}
                        </button>
                    ))}
                    {end < pages - 1 && <span className="px-1 text-small-label">…</span>}
                    {end < pages && (
                        <Button variant="ghost" size="sm" onClick={() => onPage(pages)}>
                            {pages}
                        </Button>
                    )}
                    <Button
                        variant="secondary"
                        size="sm"
                        disabled={page >= pages}
                        onClick={() => onPage(page + 1)}
                    >
                        {t("Pagination_Next")} <ChevronRight className="h-3 w-3" />
                    </Button>
                </div>
            )}
        </footer>
    );
}
