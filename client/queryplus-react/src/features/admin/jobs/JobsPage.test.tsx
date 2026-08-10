import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { vi } from "vitest";
import { JobsPage } from "./JobsPage";
import { deleteJob, jobsSearch, submitJobForApproval } from "@/api/queries";

const { DRAFT_JOB, PENDING_OWN_JOB, PENDING_OTHER_JOB } = vi.hoisted(() => ({
    DRAFT_JOB: {
        id: 1,
        name: "Nightly Backup",
        jobType: 1,
        approvalStatus: 1,
        enabled: false,
        cronExpression: "0 2 * * *",
        runAsUser: "svc-job",
        createdBy: "alice",
        approvedBy: null,
        updatedAt: null,
    },
    PENDING_OWN_JOB: {
        id: 2,
        name: "Own Pending Job",
        jobType: 2,
        approvalStatus: 2,
        enabled: false,
        cronExpression: "0 3 * * *",
        runAsUser: "svc-job",
        createdBy: "alice",
        approvedBy: null,
        updatedAt: null,
    },
    PENDING_OTHER_JOB: {
        id: 3,
        name: "Other Pending Job",
        jobType: 1,
        approvalStatus: 2,
        enabled: false,
        cronExpression: "0 4 * * *",
        runAsUser: "svc-job",
        createdBy: "bob",
        approvedBy: null,
        updatedAt: null,
    },
}));

vi.mock("@/api/queries", () => ({
    authQuery: {
        queryKey: ["auth", "user"],
        queryFn: () =>
            Promise.resolve({
                username: "alice",
                roles: ["ROLE_JOB_WRITE", "ROLE_JOB_APPROVE"],
                isAuthenticated: true,
            }),
    },
    jobsSearch: vi.fn().mockResolvedValue({
        items: [DRAFT_JOB, PENDING_OWN_JOB, PENDING_OTHER_JOB],
        totalCount: 3,
        page: 1,
        pageSize: 20,
        totalPages: 1,
    }),
    jobRunRequestQuery: vi.fn((requestId: number) => ({
        queryKey: ["jobs", "runs", "requests", requestId],
        queryFn: () =>
            Promise.resolve({
                id: requestId,
                jobDefinitionId: 1,
                requestedBy: "alice",
                requestedAt: "2026-01-01T00:00:00Z",
                consumedAt: null,
                jobRunId: null,
            }),
    })),
    deleteJob: vi.fn().mockResolvedValue(undefined),
    submitJobForApproval: vi.fn().mockResolvedValue(undefined),
    approveJob: vi.fn().mockResolvedValue(undefined),
    rejectJob: vi.fn().mockResolvedValue(undefined),
    setJobEnabled: vi.fn().mockResolvedValue(undefined),
    runJobNow: vi.fn().mockResolvedValue(undefined),
}));

function renderWithProviders() {
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    return render(
        <QueryClientProvider client={queryClient}>
            <MemoryRouter initialEntries={["/admin/jobs"]}>
                <JobsPage />
            </MemoryRouter>
        </QueryClientProvider>,
    );
}

test("renders the jobs table with data from the search query", async () => {
    renderWithProviders();

    await screen.findByText("Nightly Backup");
    const table = screen.getByRole("table");
    expect(within(table).getByText("0 2 * * *")).toBeInTheDocument();
    expect(within(table).getAllByText("svc-job").length).toBe(3);
});

test("a Draft job with write role shows Submit and Delete but not Approve/Reject/Run Now", async () => {
    renderWithProviders();
    await screen.findByText("Nightly Backup");

    const row = screen.getByText("Nightly Backup").closest("tr");
    expect(row).not.toBeNull();
    const scoped = within(row as HTMLElement);
    expect(scoped.getByRole("button", { name: /Submit/ })).toBeInTheDocument();
    expect(scoped.getByRole("button", { name: /Delete/ })).toBeInTheDocument();
    expect(scoped.queryByRole("button", { name: /Approve/ })).not.toBeInTheDocument();
    expect(scoped.queryByRole("button", { name: /Run Now/ })).not.toBeInTheDocument();
});

test("clicking Submit calls submitJobForApproval for that job", async () => {
    renderWithProviders();
    await screen.findByText("Nightly Backup");

    const row = screen.getByText("Nightly Backup").closest("tr") as HTMLElement;
    await userEvent.click(within(row).getByRole("button", { name: /Submit/ }));

    expect(submitJobForApproval).toHaveBeenCalledWith(1);
});

test("clicking Delete opens a confirmation dialog, and confirming calls deleteJob", async () => {
    renderWithProviders();
    await screen.findByText("Nightly Backup");

    const row = screen.getByText("Nightly Backup").closest("tr") as HTMLElement;
    await userEvent.click(within(row).getByRole("button", { name: /Delete/ }));
    expect(await screen.findByRole("button", { name: "Yes" })).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Yes" }));

    expect(deleteJob).toHaveBeenCalledWith(1);
});

test("Approve is hidden on the current user's own PendingApproval job but shown on someone else's", async () => {
    renderWithProviders();
    await screen.findByText("Own Pending Job");

    const ownRow = screen.getByText("Own Pending Job").closest("tr") as HTMLElement;
    expect(within(ownRow).queryByRole("button", { name: /Approve/ })).not.toBeInTheDocument();
    expect(within(ownRow).getByRole("button", { name: /Reject/ })).toBeInTheDocument();

    const otherRow = screen.getByText("Other Pending Job").closest("tr") as HTMLElement;
    expect(within(otherRow).getByRole("button", { name: /Approve/ })).toBeInTheDocument();
});

test("typing a filter and clicking Search re-queries with that filter, reset to page 1", async () => {
    renderWithProviders();
    await screen.findByText("Nightly Backup");
    vi.mocked(jobsSearch).mockClear();

    await userEvent.type(screen.getByLabelText("Name"), "Backup");
    await userEvent.click(screen.getByText("Search"));

    expect(jobsSearch).toHaveBeenCalled();
    const params = vi.mocked(jobsSearch).mock.calls.at(-1)?.[0];
    expect(params?.get("name")).toBe("Backup");
    expect(params?.get("pageNumber")).toBe("1");
});
