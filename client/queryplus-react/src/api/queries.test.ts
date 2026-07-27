import { vi, test, expect } from "vitest";
import {
    authQuery,
    accessibleProceduresQuery,
    procedureQuery,
    procedureParametersQuery,
    categoryLookupQuery,
    procedureLookupQuery,
    categoryQuery,
    execute,
    queueExport,
    exportStatus,
    categoriesSearch,
    proceduresSearch,
    executionLogsSearch,
} from "./queries";
import { apiFetch } from "./client";

vi.mock("./client", () => ({
    apiFetch: vi.fn().mockResolvedValue({ success: true }),
}));

test("authQuery invokes apiFetch correctly", async () => {
    await authQuery.queryFn();
    expect(apiFetch).toHaveBeenCalledWith("/api/auth/user");
});

test("accessibleProceduresQuery invokes apiFetch correctly", async () => {
    await accessibleProceduresQuery.queryFn();
    expect(apiFetch).toHaveBeenCalledWith("/api/procedures/accessible");
});

test("procedureQuery generates valid query options", async () => {
    const opts = procedureQuery(5);
    expect(opts.queryKey).toEqual(["procedures", 5]);
    expect(opts.enabled).toBe(true);

    await opts.queryFn();
    expect(apiFetch).toHaveBeenCalledWith("/api/procedures/5");
});

test("procedureParametersQuery generates valid options", async () => {
    const opts = procedureParametersQuery(10);
    await opts.queryFn();
    expect(apiFetch).toHaveBeenCalledWith("/api/procedures/10/parameters");
});

test("categoryLookupQuery and procedureLookupQuery invoke apiFetch", async () => {
    await categoryLookupQuery.queryFn();
    expect(apiFetch).toHaveBeenCalledWith("/api/categories/lookup");

    await procedureLookupQuery.queryFn();
    expect(apiFetch).toHaveBeenCalledWith("/api/procedures/lookup");
});

test("categoryQuery generates valid query options", async () => {
    const opts = categoryQuery(2);
    await opts.queryFn();
    expect(apiFetch).toHaveBeenCalledWith("/api/categories/2");
});

test("execute, queueExport, and exportStatus call apiFetch", async () => {
    await execute({ procedureId: 1, parameters: {} });
    expect(apiFetch).toHaveBeenCalledWith("/api/execute", expect.objectContaining({ method: "POST" }));

    await queueExport({ procedureId: 1, parameters: {} });
    expect(apiFetch).toHaveBeenCalledWith("/api/exports", expect.objectContaining({ method: "POST" }));

    await exportStatus("job-123");
    expect(apiFetch).toHaveBeenCalledWith("/api/exports/job-123");
});

test("search functions query correct API endpoints with params", async () => {
    const params = new URLSearchParams({ pageNumber: "1" });

    await categoriesSearch(params);
    expect(apiFetch).toHaveBeenCalledWith("/api/categories?pageNumber=1");

    await proceduresSearch(params);
    expect(apiFetch).toHaveBeenCalledWith("/api/procedures?pageNumber=1");

    await executionLogsSearch(params);
    expect(apiFetch).toHaveBeenCalledWith("/api/execution-logs?pageNumber=1");
});
