import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { vi } from "vitest";
import { ExecutionLogsPage } from "./ExecutionLogsPage";
import { executionLogsSearch } from "@/api/queries";

const { LOG } = vi.hoisted(() => ({
    LOG: {
        id: 1,
        procedureId: 5,
        procedureCaption: "Sales Report",
        username: "alice",
        ipAddress: "10.0.0.1",
        executionStart: "2026-03-05T10:00:00Z",
        executionEnd: "2026-03-05T10:00:01Z",
        success: true,
        errorMessage: null,
        rowCount: 42,
    },
}));

vi.mock("@/api/queries", () => ({
    executionLogsSearch: vi.fn().mockResolvedValue({
        items: [LOG],
        totalCount: 1,
        page: 1,
        pageSize: 20,
        totalPages: 1,
    }),
}));

vi.mock("@/api/client", () => ({
    apiFetch: vi.fn().mockResolvedValue([{ id: 5, caption: "Sales Report (lookup)" }]),
}));

function renderWithClient() {
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    return render(
        <QueryClientProvider client={queryClient}>
            <ExecutionLogsPage />
        </QueryClientProvider>,
    );
}

test("renders execution log rows with formatted duration and status", async () => {
    renderWithClient();

    expect(await screen.findByText("Sales Report")).toBeInTheDocument();
    const table = screen.getByRole("table");
    expect(within(table).getByText("alice")).toBeInTheDocument();
    expect(within(table).getByText("10.0.0.1")).toBeInTheDocument();
    expect(within(table).getByText("1.0s")).toBeInTheDocument(); // 1000ms duration
    expect(within(table).getByText("Success")).toBeInTheDocument();
    expect(within(table).getByText("42")).toBeInTheDocument();
});

test("shows a placeholder duration when the execution has not finished", async () => {
    vi.mocked(executionLogsSearch).mockResolvedValueOnce({
        items: [{ ...LOG, executionEnd: null, success: false, errorMessage: "Timeout" }],
        totalCount: 1,
        page: 1,
        pageSize: 20,
        totalPages: 1,
    });

    renderWithClient();

    expect(await screen.findByText("Timeout")).toBeInTheDocument();
    const table = screen.getByRole("table");
    expect(within(table).getByText("Failed")).toBeInTheDocument();
    expect(within(table).getAllByText("—").length).toBeGreaterThan(0); // duration placeholder
});

test("typing a username filter and clicking Search re-queries with that filter, reset to page 1", async () => {
    renderWithClient();
    await screen.findByText("Sales Report");
    vi.mocked(executionLogsSearch).mockClear();

    await userEvent.type(screen.getByLabelText("Username"), "alice");
    await userEvent.click(screen.getByText("Search"));

    expect(executionLogsSearch).toHaveBeenCalled();
    const params = vi.mocked(executionLogsSearch).mock.calls.at(-1)?.[0];
    expect(params?.get("username")).toBe("alice");
    expect(params?.get("pageNumber")).toBe("1");
});

test("Clear resets filters back to empty and re-queries", async () => {
    renderWithClient();
    await screen.findByText("Sales Report");

    await userEvent.type(screen.getByLabelText("Username"), "alice");
    await userEvent.click(screen.getByText("Search"));
    vi.mocked(executionLogsSearch).mockClear();

    await userEvent.click(screen.getByText("Clear"));

    expect(screen.getByLabelText("Username")).toHaveValue("");
    expect(executionLogsSearch).toHaveBeenCalled();
    const params = vi.mocked(executionLogsSearch).mock.calls.at(-1)?.[0];
    expect(params?.get("username")).toBeNull();
});
