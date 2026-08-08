import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { CategoriesPage, categoryFormToApi } from "./CategoriesPage";
import { vi } from "vitest";
import { categoriesSearch } from "@/api/queries";
import { apiFetch } from "@/api/client";

vi.mock("@/api/queries", () => ({
    categoriesSearch: vi.fn().mockResolvedValue({
        items: [
            { id: 1, description: "Finance", createdAt: "2026-01-01T00:00:00Z", updatedAt: null },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 20,
        totalPages: 1,
    }),
    categoryQuery: vi.fn((id: number) => ({
        queryKey: ["category", id],
        queryFn: () =>
            Promise.resolve({ id: 1, description: "Finance", createdAt: "2026-01-01T00:00:00Z" }),
    })),
}));

vi.mock("@/api/client", () => ({
    apiFetch: vi.fn().mockResolvedValue(undefined),
}));

function renderWithClient(component: React.ReactNode) {
    const queryClient = new QueryClient({
        defaultOptions: { queries: { retry: false } },
    });
    return render(<QueryClientProvider client={queryClient}>{component}</QueryClientProvider>);
}

test("categoryFormToApi trims values and preserves id", () => {
    expect(categoryFormToApi({ description: "  Finance  " }, 4)).toEqual({
        id: 4,
        description: "Finance",
    });
});

test("renders CategoriesPage table and action buttons", async () => {
    renderWithClient(<CategoriesPage />);

    expect(await screen.findByText("Finance")).toBeInTheDocument();
    expect(screen.getByText("Search")).toBeInTheDocument();
    expect(screen.getByText("New")).toBeInTheDocument();
});

test("typing a filter and clicking Search re-queries with that filter, reset to page 1", async () => {
    renderWithClient(<CategoriesPage />);
    await screen.findByText("Finance");
    vi.mocked(categoriesSearch).mockClear();

    await userEvent.type(screen.getByLabelText("Description"), "Fin");
    await userEvent.click(screen.getByText("Search"));

    expect(categoriesSearch).toHaveBeenCalled();
    const params = vi.mocked(categoriesSearch).mock.calls.at(-1)?.[0];
    expect(params?.get("description")).toBe("Fin");
    expect(params?.get("pageNumber")).toBe("1");
});

test("editing a category pre-fills the dialog and saves via PUT", async () => {
    renderWithClient(<CategoriesPage />);
    await screen.findByText("Finance");

    await userEvent.click(screen.getByRole("button", { name: "Edit" }));
    const dialog = await screen.findByRole("dialog");
    expect(within(dialog).getByText("Edit category")).toBeInTheDocument();
    const input = await within(dialog).findByDisplayValue("Finance");

    await userEvent.clear(input);
    await userEvent.type(input, "Finance Updated");
    await userEvent.click(within(dialog).getByRole("button", { name: "Save" }));

    expect(apiFetch).toHaveBeenCalledWith(
        "/api/categories/1",
        expect.objectContaining({
            method: "PUT",
            body: JSON.stringify({ id: 1, description: "Finance Updated" }),
        }),
    );
});

test("creating a category with a blank description blocks submission and shows a validation error", async () => {
    renderWithClient(<CategoriesPage />);
    await screen.findByText("Finance");
    vi.mocked(apiFetch).mockClear();

    await userEvent.click(screen.getByRole("button", { name: "New" }));
    const dialog = await screen.findByRole("dialog");
    expect(within(dialog).getByText("New category")).toBeInTheDocument();

    const input = within(dialog).getByLabelText("Description");
    await userEvent.clear(input);
    await userEvent.click(within(dialog).getByRole("button", { name: "Save" }));

    // Blank description fails the zod schema client-side: the dialog stays open showing an
    // inline field error instead of submitting.
    expect(await within(dialog).findByText(/./, { selector: "span.text-danger" })).toBeInTheDocument();
    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(apiFetch).not.toHaveBeenCalled();
});

test("deleting a category opens a confirmation dialog, and confirming calls DELETE", async () => {
    renderWithClient(<CategoriesPage />);
    await screen.findByText("Finance");
    vi.mocked(apiFetch).mockClear();

    await userEvent.click(screen.getByRole("button", { name: "Delete" }));
    expect(await screen.findByText("Are you sure you want to delete this record?")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Yes" }));

    expect(apiFetch).toHaveBeenCalledWith("/api/categories/1", { method: "DELETE" });
    expect(screen.queryByRole("button", { name: "Yes" })).not.toBeInTheDocument();
});
