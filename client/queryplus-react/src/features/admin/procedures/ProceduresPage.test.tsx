import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { vi } from "vitest";
import { ProceduresPage } from "./ProceduresPage";
import { proceduresSearch } from "@/api/queries";
import { apiFetch } from "@/api/client";

const { PROCEDURE } = vi.hoisted(() => ({
    PROCEDURE: {
        id: 1,
        caption: "Sales Report",
        categoryDescription: "Sales",
        databaseName: "SalesDB",
        procedureName: "dbo.sp_sales",
        roleEntitlement: "user",
        enabled: true,
    },
}));

vi.mock("@/api/queries", () => ({
    categoryLookupQuery: {
        queryKey: ["categories", "lookup"],
        queryFn: () => Promise.resolve([{ id: 1, description: "Sales" }]),
    },
    proceduresSearch: vi.fn().mockResolvedValue({
        items: [PROCEDURE],
        totalCount: 1,
        page: 1,
        pageSize: 20,
        totalPages: 1,
    }),
}));

vi.mock("@/api/client", () => ({
    apiFetch: vi.fn().mockResolvedValue(undefined),
}));

function renderWithProviders() {
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    return render(
        <QueryClientProvider client={queryClient}>
            <MemoryRouter initialEntries={["/admin/procedures"]}>
                <ProceduresPage />
            </MemoryRouter>
        </QueryClientProvider>,
    );
}

test("renders the procedures table with data from the search query", async () => {
    renderWithProviders();

    await screen.findByText("Sales Report");
    const table = screen.getByRole("table");
    expect(within(table).getByText("dbo.sp_sales")).toBeInTheDocument();
    expect(within(table).getByRole("cell", { name: "Sales" })).toBeInTheDocument();
});

test("clicking Delete opens a confirmation dialog, and confirming calls DELETE and closes it", async () => {
    renderWithProviders();
    await screen.findByText("Sales Report");

    await userEvent.click(screen.getByRole("button", { name: "Delete" }));
    expect(await screen.findByText("Are you sure you want to delete this record?")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Yes" }));

    expect(apiFetch).toHaveBeenCalledWith("/api/procedures/1", { method: "DELETE" });
    expect(screen.queryByRole("button", { name: "Yes" })).not.toBeInTheDocument();
});

test("clicking Delete then Cancel closes the dialog without deleting", async () => {
    renderWithProviders();
    await screen.findByText("Sales Report");
    vi.mocked(apiFetch).mockClear();

    await userEvent.click(screen.getByRole("button", { name: "Delete" }));
    await screen.findByRole("button", { name: "Cancel" });
    await userEvent.click(screen.getByRole("button", { name: "Cancel" }));

    expect(apiFetch).not.toHaveBeenCalled();
    expect(screen.queryByRole("button", { name: "Cancel" })).not.toBeInTheDocument();
});

test("typing a filter and clicking Search re-queries with that filter, reset to page 1", async () => {
    renderWithProviders();
    await screen.findByText("Sales Report");
    vi.mocked(proceduresSearch).mockClear();

    await userEvent.type(screen.getByLabelText("Caption"), "Sales");
    await userEvent.click(screen.getByText("Search"));

    expect(proceduresSearch).toHaveBeenCalled();
    const params = vi.mocked(proceduresSearch).mock.calls.at(-1)?.[0];
    expect(params?.get("caption")).toBe("Sales");
    expect(params?.get("pageNumber")).toBe("1");
});
