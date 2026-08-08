import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { CategoriesPage, categoryFormToApi } from "./CategoriesPage";
import { vi } from "vitest";
import { categoriesSearch } from "@/api/queries";

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
    apiFetch: vi.fn(),
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
