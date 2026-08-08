import { queryOptions } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type {
    AuthUser,
    CategoryDetail,
    CategoryListItem,
    ExecuteRequest,
    ExecuteResponse,
    ExecutionLog,
    ExportJob,
    ExportRequest,
    PagedResult,
    ProcedureDetail,
    ProcedureListItem,
    ProcedureLookup,
} from "./types";

export const authQuery = queryOptions({
    queryKey: ["auth", "user"],
    queryFn: () => apiFetch<AuthUser>("/api/auth/user"),
    staleTime: 30_000,
});

export const accessibleProceduresQuery = queryOptions({
    queryKey: ["procedures", "accessible"],
    queryFn: () => apiFetch<ProcedureLookup[]>("/api/procedures/accessible"),
});

export const procedureQuery = (id: number) =>
    queryOptions({
        queryKey: ["procedures", id],
        queryFn: () => apiFetch<ProcedureDetail>(`/api/procedures/${id}`),
        enabled: id > 0,
    });

export const procedureParametersQuery = (id: number) =>
    queryOptions({
        queryKey: ["procedures", id, "parameters"],
        queryFn: () => apiFetch<ProcedureDetail["parameters"]>(`/api/procedures/${id}/parameters`),
        enabled: id > 0,
        staleTime: 5 * 60_000,
    });

export const categoryLookupQuery = queryOptions({
    queryKey: ["categories", "lookup"],
    queryFn: () => apiFetch<CategoryListItem[]>("/api/categories/lookup"),
});

export const procedureLookupQuery = queryOptions({
    queryKey: ["procedures", "lookup"],
    queryFn: () => apiFetch<ProcedureLookup[]>("/api/procedures/lookup"),
});

export const connectionsLookupQuery = queryOptions({
    queryKey: ["procedures", "connections"],
    queryFn: () => apiFetch<string[]>("/api/procedures/connections"),
});

export const categoryQuery = (id: number) =>
    queryOptions({
        queryKey: ["categories", id],
        queryFn: () => apiFetch<CategoryDetail>(`/api/categories/${id}`),
        enabled: id > 0,
    });

export function execute(request: ExecuteRequest): Promise<ExecuteResponse> {
    return apiFetch("/api/execute", { method: "POST", body: JSON.stringify(request) });
}

export function queueExport(request: ExportRequest): Promise<ExportJob> {
    return apiFetch("/api/exports", { method: "POST", body: JSON.stringify(request) });
}

export function exportStatus(jobId: string): Promise<ExportJob> {
    return apiFetch(`/api/exports/${jobId}`);
}

export function categoriesSearch(params: URLSearchParams): Promise<PagedResult<CategoryListItem>> {
    return apiFetch(`/api/categories?${params.toString()}`);
}

export function proceduresSearch(params: URLSearchParams): Promise<PagedResult<ProcedureListItem>> {
    return apiFetch(`/api/procedures?${params.toString()}`);
}

export function executionLogsSearch(params: URLSearchParams): Promise<PagedResult<ExecutionLog>> {
    return apiFetch(`/api/execution-logs?${params.toString()}`);
}
