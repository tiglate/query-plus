import { useQuery } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import type { PagedResult } from "@/api/types";

/**
 * Shared search/filter/pagination state machine for the admin list pages
 * (Categories/Procedures/ExecutionLogs), which otherwise each hand-rolled an identical
 * draft/filter/page/pageSize + URLSearchParams + query-key pattern. Filters are a flat
 * string-keyed record; empty values are omitted from the request.
 */
export function useAdminSearch<TFilters extends Record<string, string>, TItem>(
    resource: string,
    emptyFilters: TFilters,
    searchFn: (params: URLSearchParams) => Promise<PagedResult<TItem>>,
) {
    const [draft, setDraft] = useState<TFilters>(emptyFilters);
    const [filter, setFilter] = useState<TFilters>(emptyFilters);
    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(20);

    const params = new URLSearchParams({ pageNumber: String(page), pageSize: String(pageSize) });
    for (const key of Object.keys(filter) as Array<keyof TFilters & string>) {
        const value = filter[key];
        if (value) params.set(key, value);
    }
    const paramsKey = params.toString();

    const query = useQuery({
        queryKey: [resource, "search", paramsKey],
        queryFn: () => searchFn(params),
    });

    // The backend clamps out-of-range page numbers (e.g. filtering shrinks the result set
    // below the currently-viewed page) - follow that correction instead of silently
    // requesting a page the server already told us doesn't exist.
    useEffect(() => {
        if (query.data && query.data.page !== page) setPage(query.data.page);
        // eslint-disable-next-line react-hooks/exhaustive-deps -- only react to the server's page
    }, [query.data?.page]);

    const updateDraft = (key: keyof TFilters, value: string) =>
        setDraft((current) => ({ ...current, [key]: value }));

    const search = () => {
        const trimmed = Object.fromEntries(
            Object.entries(draft).map(([key, value]) => [key, value.trim()]),
        ) as TFilters;
        setFilter(trimmed);
        setPage(1);
    };

    const clear = () => {
        setDraft(emptyFilters);
        setFilter(emptyFilters);
        setPage(1);
    };

    const changePageSize = (size: number) => {
        setPageSize(size);
        setPage(1);
    };

    return {
        draft,
        updateDraft,
        search,
        clear,
        page,
        setPage,
        pageSize,
        changePageSize,
        query,
        total: query.data?.totalCount ?? 0,
    };
}
