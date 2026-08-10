import { fireEvent, render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { vi } from "vitest";
import { JobEditorPage, jobFormToApi, type JobFormValues } from "./JobEditorPage";

const routeState = vi.hoisted(() => ({ id: "1" }));

vi.mock("react-router-dom", async () => {
    const actual = await vi.importActual<typeof import("react-router-dom")>("react-router-dom");
    return {
        ...actual,
        useParams: () => ({ id: routeState.id }),
        useSearchParams: () =>
            [new URLSearchParams(), vi.fn()] as ReturnType<typeof actual.useSearchParams>,
        useNavigate: () => vi.fn(),
    };
});

vi.mock("@/api/queries", () => ({
    authQuery: {
        queryKey: ["auth", "user"],
        queryFn: () =>
            Promise.resolve({
                username: "alice",
                roles: ["ROLE_JOB_WRITE"],
                isAuthenticated: true,
            }),
    },
    jobQuery: vi.fn((id: number) => ({
        queryKey: ["jobs", id],
        // Mirrors the real jobQuery's `enabled: id > 0` (api/queries.ts) - without it, create mode
        // (id ?? 0 === 0) would still fetch and resolve this fake "Nightly Backup" job, racing with
        // and clobbering whatever a test has typed into the still-empty create form.
        enabled: id > 0,
        queryFn: () =>
            Promise.resolve({
                id,
                name: id === 2 ? "Legacy Job" : "Nightly Backup",
                description: "Backs up the warehouse",
                jobType: 1,
                scriptPath: "/opt/queryplus/scripts/backup.sh",
                scriptSha256: null,
                cronExpression: "0 2 * * *",
                runAsUser: id === 2 ? "legacy-user" : "svc-job",
                memoryLimitMb: 512,
                maxDurationMinutes: 60,
                enabled: false,
                approvalStatus: 1,
                createdBy: "alice",
                approvedBy: null,
                approvedAt: null,
                rejectionReason: null,
                notifyEmails: "ops@example.com",
                createdAt: "2026-01-01T00:00:00Z",
                updatedAt: null,
            }),
    })),
    jobRunAsUsersQuery: {
        queryKey: ["jobs", "run-as-users"],
        queryFn: () => Promise.resolve(["svc-job", "svc-other"]),
    },
    createJob: vi.fn(),
    updateJob: vi.fn(),
    uploadJobScript: vi.fn(),
}));

import { MemoryRouter } from "react-router-dom";

function renderWithClient(component: React.ReactNode, initialEntry = "/admin/jobs/1") {
    const queryClient = new QueryClient({
        defaultOptions: { queries: { retry: false } },
    });
    return render(
        <QueryClientProvider client={queryClient}>
            <MemoryRouter initialEntries={[initialEntry]}>{component}</MemoryRouter>
        </QueryClientProvider>,
    );
}

const form: JobFormValues = {
    name: "  Nightly Backup ",
    description: " Backs up the warehouse ",
    jobType: 1,
    cronExpression: " 0 2 * * * ",
    runAsUser: " svc-job ",
    memoryLimitMb: 512,
    maxDurationMinutes: 60,
    notifyEmails: " ops@example.com, oncall@example.com ",
};

test("jobFormToApi trims values, coerces jobType, and normalizes notifyEmails", () => {
    const result = jobFormToApi(form, 9);
    expect(result).toEqual({
        id: 9,
        name: "Nightly Backup",
        description: "Backs up the warehouse",
        jobType: 1,
        cronExpression: "0 2 * * *",
        runAsUser: "svc-job",
        memoryLimitMb: 512,
        maxDurationMinutes: 60,
        notifyEmails: "ops@example.com, oncall@example.com",
    });
});

test("jobFormToApi rejects an invalid email in notifyEmails", () => {
    expect(() => jobFormToApi({ ...form, notifyEmails: "not-an-email" })).toThrow();
});

test("jobFormToApi treats blank/null description and notifyEmails as null", () => {
    const result = jobFormToApi({ ...form, description: "  ", notifyEmails: null }, undefined);
    expect(result.description).toBeNull();
    expect(result.notifyEmails).toBeNull();
});

test("renders JobEditorPage form with job values", async () => {
    renderWithClient(<JobEditorPage />);

    expect(await screen.findByDisplayValue("Nightly Backup")).toBeInTheDocument();
    expect(screen.getByDisplayValue("0 2 * * *")).toBeInTheDocument();
    expect(screen.getByText("Save")).toBeInTheDocument();
});

test("script file can be selected in create mode, before the job is saved", async () => {
    routeState.id = "new";
    try {
        renderWithClient(<JobEditorPage />, "/admin/jobs/new");

        // Waits for the auth query (canWrite/readOnly) to resolve - the "Save" button only
        // renders once readOnly is false, which is also what un-disables the file input below.
        await screen.findByText("Save");
        await screen.findByText(
            "Select a script - it will be uploaded automatically when you save.",
        );
        const fileInput = document.querySelector('input[type="file"]');
        expect(fileInput).not.toBeDisabled();
        // No standalone Upload button in create mode - there's no job id to upload against yet;
        // the file is uploaded automatically once Save creates the job.
        expect(screen.queryByText("Upload")).not.toBeInTheDocument();

        const file = new File(["echo hi"], "backup.sh", { type: "text/x-sh" });
        fireEvent.change(fileInput as HTMLInputElement, { target: { files: [file] } });

        await screen.findByText('"backup.sh" will be uploaded when you save.');
    } finally {
        routeState.id = "1";
    }
});

test("script upload control is enabled once the job has a real id", async () => {
    renderWithClient(<JobEditorPage />);

    await screen.findByDisplayValue("Nightly Backup");
    const fileInput = document.querySelector('input[type="file"]');
    expect(fileInput).not.toBeDisabled();
    expect(screen.getByText("Upload").closest("button")).toBeDisabled(); // no file picked yet
});

test("auto-uploads the selected script right after creating a new job", async () => {
    routeState.id = "new";
    const { createJob, uploadJobScript } = await import("@/api/queries");
    const createJobMock = vi.mocked(createJob);
    const uploadJobScriptMock = vi.mocked(uploadJobScript);
    createJobMock.mockResolvedValue({ id: 42 } as never);
    uploadJobScriptMock.mockResolvedValue({} as never);

    try {
        renderWithClient(<JobEditorPage />, "/admin/jobs/new");

        // Waits for the auth query (canWrite/readOnly) to resolve before touching the (until
        // then still-disabled) file input.
        await screen.findByText("Save");
        await screen.findByText(
            "Select a script - it will be uploaded automatically when you save.",
        );

        const file = new File(["echo hi"], "backup.sh", { type: "text/x-sh" });
        fireEvent.change(document.querySelector('input[type="file"]') as HTMLInputElement, {
            target: { files: [file] },
        });
        await screen.findByText('"backup.sh" will be uploaded when you save.');

        // "Cron expression" uses a plain CSS-class selector, not getByLabelText: its Field wraps
        // both the Input and the "Build" button, and <button> is a labelable element too, so
        // getByLabelText there is ambiguous.
        fireEvent.change(screen.getByLabelText("Name"), { target: { value: "New Job" } });
        fireEvent.change(document.querySelector("input.font-mono") as HTMLInputElement, {
            target: { value: "0 3 * * *" },
        });
        await screen.findByRole("option", { name: "svc-job" }); // wait for the catalog to load
        fireEvent.change(screen.getByLabelText("Run as user"), {
            target: { value: "svc-job" },
        });

        fireEvent.click(screen.getByText("Save"));

        await vi.waitFor(() => expect(createJobMock).toHaveBeenCalled());
        await vi.waitFor(() => expect(uploadJobScriptMock).toHaveBeenCalledWith(42, file));
    } finally {
        routeState.id = "1";
    }
});

test("Build button opens the cron expression builder dialog", async () => {
    renderWithClient(<JobEditorPage />);

    await screen.findByDisplayValue("Nightly Backup");
    screen.getByText("Build").click();

    expect(await screen.findByText("Cron expression builder")).toBeInTheDocument();
});

test("applying a cron expression from the builder updates the cron field", async () => {
    renderWithClient(<JobEditorPage />);

    await screen.findByDisplayValue("Nightly Backup");
    screen.getByText("Build").click();
    await screen.findByText("Cron expression builder");

    fireEvent.click(screen.getByRole("radio", { name: "Daily" }));
    fireEvent.change(screen.getByLabelText("Time"), { target: { value: "07:30" } });
    fireEvent.click(screen.getByRole("button", { name: "Apply" }));

    expect(await screen.findByDisplayValue("30 7 * * *")).toBeInTheDocument();
});

test("shows the job's current runAsUser even when it is absent from the catalog", async () => {
    routeState.id = "2";
    try {
        renderWithClient(<JobEditorPage />, "/admin/jobs/2");

        expect(await screen.findByDisplayValue("legacy-user")).toBeInTheDocument();
    } finally {
        routeState.id = "1";
    }
});
