import { render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
    ProcedureEditorPage,
    procedureFormToApi,
    type ProcedureFormValues,
} from "./ProcedureEditorPage";
import { vi } from "vitest";

vi.mock("react-router-dom", async () => {
    const actual = await vi.importActual<typeof import("react-router-dom")>("react-router-dom");
    return {
        ...actual,
        useParams: () => ({ id: "1" }),
        useSearchParams: () => [new URLSearchParams(), vi.fn()] as any,
        useNavigate: () => vi.fn(),
    };
});

vi.mock("@/api/queries", () => ({
    categoryLookupQuery: {
        queryKey: ["categories", "lookup"],
        queryFn: () => Promise.resolve([{ id: 1, description: "General" }]),
    },
    procedureQuery: vi.fn((id: number) => ({
        queryKey: ["procedure", id],
        queryFn: () =>
            Promise.resolve({
                id: 1,
                categoryId: 1,
                caption: "Sales Report",
                databaseName: "SalesDB",
                procedureName: "dbo.sp_sales",
                enabled: true,
                supportsPagination: true,
                roleEntitlement: "user",
                description: "Sales procedure",
                parameters: [],
                columns: [],
            }),
    })),
}));

import { MemoryRouter } from "react-router-dom";

function renderWithClient(component: React.ReactNode) {
    const queryClient = new QueryClient({
        defaultOptions: { queries: { retry: false } },
    });
    return render(
        <QueryClientProvider client={queryClient}>
            <MemoryRouter initialEntries={["/admin/procedures/1"]}>{component}</MemoryRouter>
        </QueryClientProvider>,
    );
}

const form: ProcedureFormValues = {
    categoryId: 3,
    caption: "  Report ",
    databaseName: " Main ",
    procedureName: " dbo.Report ",
    roleEntitlement: " analyst ",
    enabled: true,
    supportsPagination: false,
    description: " ",
    parameters: [
        {
            caption: " Status ",
            name: " @Status ",
            parameterType: 6,
            defaultValue: " A ",
            comboValues: '["A", "B"]',
            isRequired: true,
        },
    ],
    columns: [
        {
            technicalName: " Code ",
            caption: " Code ",
            alignment: 2,
            formatMask: " ",
            visible: true,
        },
    ],
};

test("procedure form transforms local arrays and canonicalizes combo JSON", () => {
    const result = procedureFormToApi(form, 9);
    expect(result).toMatchObject({
        id: 9,
        caption: "Report",
        databaseName: "Main",
        description: null,
    });
    expect(result.parameters[0]).toMatchObject({ name: "@Status", comboValues: '["A","B"]' });
    expect(result.columns[0]).toMatchObject({ technicalName: "Code", formatMask: null });
});

test("renders ProcedureEditorPage form with procedure values", async () => {
    renderWithClient(<ProcedureEditorPage />);

    expect(await screen.findByDisplayValue("Sales Report")).toBeInTheDocument();
    expect(screen.getByText("Save")).toBeInTheDocument();
});
