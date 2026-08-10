import { queryOptions } from "@tanstack/react-query";
import { apiFetch } from "./client";
import type {
    ApproveJobRequest,
    AuthUser,
    CategoryDetail,
    CategoryListItem,
    ExecuteRequest,
    ExecuteResponse,
    ExecutionLog,
    ExportJob,
    ExportRequest,
    JobDetail,
    JobInput,
    JobListItem,
    JobLogStream,
    JobRunDetail,
    JobRunListItem,
    JobRunRequest,
    PagedResult,
    ProcedureDetail,
    ProcedureListItem,
    ProcedureLookup,
    RejectJobRequest,
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

export function jobsSearch(params: URLSearchParams): Promise<PagedResult<JobListItem>> {
    return apiFetch(`/api/jobs?${params.toString()}`);
}

export const jobQuery = (id: number) =>
    queryOptions({
        queryKey: ["jobs", id],
        queryFn: () => apiFetch<JobDetail>(`/api/jobs/${id}`),
        enabled: id > 0,
    });

export function jobRunsSearch(params: URLSearchParams): Promise<PagedResult<JobRunListItem>> {
    return apiFetch(`/api/jobs/runs?${params.toString()}`);
}

export const jobRunQuery = (id: number) =>
    queryOptions({
        queryKey: ["jobs", "runs", id],
        queryFn: () => apiFetch<JobRunDetail>(`/api/jobs/runs/${id}`),
        enabled: id > 0,
    });

export const jobRunRequestQuery = (requestId: number) =>
    queryOptions({
        queryKey: ["jobs", "runs", "requests", requestId],
        queryFn: () => apiFetch<JobRunRequest>(`/api/jobs/runs/requests/${requestId}`),
        enabled: requestId > 0,
    });

export const jobRunLogQuery = (runId: number, stream: JobLogStream) =>
    queryOptions({
        queryKey: ["jobs", "runs", runId, "logs", stream],
        queryFn: () => apiFetch<string>(`/api/jobs/runs/${runId}/logs/${stream}`),
        enabled: runId > 0,
    });

export function createJob(input: JobInput): Promise<JobDetail> {
    return apiFetch("/api/jobs", { method: "POST", body: JSON.stringify(input) });
}

export function updateJob(id: number, input: JobInput): Promise<JobDetail> {
    return apiFetch(`/api/jobs/${id}`, { method: "PUT", body: JSON.stringify(input) });
}

export function deleteJob(id: number): Promise<void> {
    return apiFetch(`/api/jobs/${id}`, { method: "DELETE" });
}

export function submitJobForApproval(id: number): Promise<JobDetail> {
    return apiFetch(`/api/jobs/${id}/submit`, { method: "POST" });
}

export function approveJob(id: number, request: ApproveJobRequest = {}): Promise<JobDetail> {
    return apiFetch(`/api/jobs/${id}/approve`, { method: "POST", body: JSON.stringify(request) });
}

export function rejectJob(id: number, request: RejectJobRequest): Promise<JobDetail> {
    return apiFetch(`/api/jobs/${id}/reject`, { method: "POST", body: JSON.stringify(request) });
}

export function setJobEnabled(id: number, enabled: boolean): Promise<JobDetail> {
    // JobDefinitionsController.SetEnabled binds [FromBody] bool directly - a raw JSON boolean,
    // not an { enabled } wrapper object.
    return apiFetch(`/api/jobs/${id}/enabled`, {
        method: "POST",
        body: JSON.stringify(enabled),
    });
}

export function runJobNow(id: number): Promise<JobRunRequest> {
    return apiFetch(`/api/jobs/${id}/run-now`, { method: "POST" });
}

export function uploadJobScript(id: number, file: File): Promise<JobDetail> {
    const body = new FormData();
    body.append("file", file);
    return apiFetch(`/api/jobs/${id}/script`, { method: "POST", body });
}

export const jobRunAsUsersQuery = queryOptions({
    queryKey: ["jobs", "run-as-users"],
    queryFn: () => apiFetch<string[]>("/api/jobs/run-as-users"),
});
