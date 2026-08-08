import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useForm } from "react-hook-form";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { beforeEach, vi } from "vitest";
import type {
    ExecuteResponse,
    ExportJob,
    ProcedureLookup,
    ProcedureParameter,
} from "@/api/types";
import { Pager } from "@/components/ui/pager";
import {
    ExportButton,
    HomePage,
    isExportEligible,
    resultSignature,
    type ParameterFormValues,
} from "./HomePage";

const { PROCEDURE, PARAMETER, executeMock, queueExportMock, exportStatusMock } = vi.hoisted(
    () => ({
        PROCEDURE: {
            id: 1,
            categoryId: 1,
            categoryDescription: "Sales",
            caption: "Sales Report",
            description: "Sales data",
            roleEntitlement: "user",
            supportsPagination: false,
        } as ProcedureLookup,
        PARAMETER: {
            id: 1,
            caption: "Category",
            name: "@Category",
            parameterType: 0,
            defaultValue: null,
            comboValues: null,
            isRequired: true,
        } as ProcedureParameter,
        executeMock: vi.fn(),
        queueExportMock: vi.fn(),
        exportStatusMock: vi.fn(),
    }),
);

vi.mock("@/api/queries", () => ({
    accessibleProceduresQuery: {
        queryKey: ["procedures", "accessible"],
        queryFn: () => Promise.resolve([PROCEDURE]),
    },
    procedureParametersQuery: (id: number) => ({
        queryKey: ["procedures", id, "parameters"],
        queryFn: () => Promise.resolve(id === PROCEDURE.id ? [PARAMETER] : []),
        enabled: id > 0,
    }),
    execute: executeMock,
    queueExport: queueExportMock,
    exportStatus: exportStatusMock,
}));

beforeEach(() => {
    executeMock.mockReset();
    queueExportMock.mockReset();
    exportStatusMock.mockReset();
});

function renderHomePage() {
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    return render(
        <QueryClientProvider client={queryClient}>
            <HomePage />
        </QueryClientProvider>,
    );
}

function sampleExecuteResponse(overrides: Partial<ExecuteResponse> = {}): ExecuteResponse {
    return {
        success: true,
        procedureId: PROCEDURE.id,
        rowCount: 1,
        supportsPagination: false,
        pageNumber: 1,
        pageSize: 50,
        totalRecords: null,
        columns: [
            { technicalName: "name", caption: "Name", alignment: 0, formatMask: null, visible: true },
        ],
        rows: [["Acme"]],
        ...overrides,
    };
}

async function selectProcedureAndFillCategory() {
    renderHomePage();
    await userEvent.click(await screen.findByRole("option", { name: /Sales Report/ }));
    await userEvent.type(screen.getByLabelText("Category"), "Widgets");
}

const result: ExecuteResponse = {
    success: true,
    procedureId: 2,
    rowCount: 1,
    supportsPagination: true,
    pageNumber: 1,
    pageSize: 50,
    totalRecords: 70,
    columns: [],
    rows: [["row"]],
};

test("parameter edits invalidate successful export eligibility", () => {
    expect(isExportEligible(result, "same", "same")).toBe(true);
    expect(isExportEligible(result, "same", "changed")).toBe(false);
});

function ExportButtonHarness({
    executedSignature,
    result: harnessResult,
}: Readonly<{ executedSignature: string | null; result: ExecuteResponse | null }>) {
    const form = useForm<ParameterFormValues>({ defaultValues: { "@Param": "initial" } });
    return (
        <>
            <input {...form.register("@Param")} />
            <ExportButton
                control={form.control}
                selectedId={1}
                result={harnessResult}
                executedSignature={executedSignature}
                pending={false}
                onExport={() => {}}
            />
        </>
    );
}

test("ExportButton re-renders live as the user types, without a parent re-render", async () => {
    const executedSignature = resultSignature(1, { "@Param": "initial" });
    render(<ExportButtonHarness executedSignature={executedSignature} result={result} />);

    const button = screen.getByRole("button", { name: /Exportar|Export/ });
    expect(button).toBeEnabled();

    await userEvent.type(screen.getByRole("textbox"), "!");

    expect(button).toBeDisabled();
});

test("pager requests the selected server page", async () => {
    const onPage = vi.fn();
    render(<Pager page={1} pageSize={50} total={70} onPage={onPage} />);
    await userEvent.click(screen.getByRole("button", { name: /Próxima|Next/ }));
    expect(onPage).toHaveBeenCalledWith(2);
});

test("pager page-number click does not submit a parent form", async () => {
    const onPage = vi.fn();
    const onSubmit = vi.fn((event: React.FormEvent<HTMLFormElement>) => event.preventDefault());
    render(
        <form onSubmit={onSubmit}>
            <Pager page={1} pageSize={50} total={150} onPage={onPage} />
        </form>,
    );
    await userEvent.click(screen.getByText("2", { selector: "button" }));
    expect(onPage).toHaveBeenCalledWith(2);
    expect(onSubmit).not.toHaveBeenCalled();
});

test("pager marks the active page with aria-current", () => {
    const onPage = vi.fn();
    render(<Pager page={3} pageSize={50} total={300} onPage={onPage} />);
    expect(screen.getByText("3", { selector: "button" })).toHaveAttribute("aria-current", "page");
    expect(screen.getByText("4", { selector: "button" })).not.toHaveAttribute("aria-current");
});

test("selecting a procedure loads its parameters and enables Execute", async () => {
    renderHomePage();

    await userEvent.click(await screen.findByRole("option", { name: /Sales Report/ }));

    expect(await screen.findByText("Category")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Execute" })).toBeEnabled();
});

test("executing with valid parameters renders the results grid", async () => {
    executeMock.mockResolvedValueOnce(sampleExecuteResponse());
    await selectProcedureAndFillCategory();

    await userEvent.click(screen.getByRole("button", { name: "Execute" }));

    expect(await screen.findByText("Acme")).toBeInTheDocument();
    expect(executeMock).toHaveBeenCalledWith(
        expect.objectContaining({
            procedureId: PROCEDURE.id,
            pageNumber: 1,
            pageSize: 50,
            parameterValues: expect.objectContaining({ "@Category": "Widgets" }),
        }),
    );
});

test("shows the execute mutation's error message", async () => {
    executeMock.mockRejectedValueOnce(new Error("Boom"));
    await selectProcedureAndFillCategory();

    await userEvent.click(screen.getByRole("button", { name: "Execute" }));

    expect(await screen.findByText("Boom")).toBeInTheDocument();
});

test("blocks execution and shows inline and summary errors when a required parameter is empty", async () => {
    renderHomePage();
    await userEvent.click(await screen.findByRole("option", { name: /Sales Report/ }));

    await userEvent.click(screen.getByRole("button", { name: "Execute" }));

    expect(await screen.findByText("Required parameter: Category")).toBeInTheDocument();
    expect(screen.getByText("Fill required parameters: Category")).toBeInTheDocument();
    expect(executeMock).not.toHaveBeenCalled();
});

test("queues an export and shows the ready state once the job completes", async () => {
    executeMock.mockResolvedValueOnce(sampleExecuteResponse());
    queueExportMock.mockResolvedValueOnce({ id: "job-1", status: 0 } satisfies ExportJob);
    exportStatusMock.mockResolvedValueOnce({ id: "job-1", status: 2 } satisfies ExportJob);
    await selectProcedureAndFillCategory();
    await userEvent.click(screen.getByRole("button", { name: "Execute" }));
    await screen.findByText("Acme");

    const exportButton = screen.getByRole("button", { name: /Export Excel/ });
    expect(exportButton).toBeEnabled();
    await userEvent.click(exportButton);

    expect(queueExportMock).toHaveBeenCalledWith(
        expect.objectContaining({
            procedureId: PROCEDURE.id,
            parameterValues: expect.objectContaining({ "@Category": "Widgets" }),
        }),
    );
    expect(await screen.findByText("File ready for download.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Download/ })).toHaveAttribute(
        "href",
        "/api/exports/job-1/download",
    );
});

test("clicking Next on the pager re-executes with the next page", async () => {
    executeMock
        .mockResolvedValueOnce(
            sampleExecuteResponse({ supportsPagination: true, totalRecords: 120 }),
        )
        .mockResolvedValueOnce(
            sampleExecuteResponse({
                supportsPagination: true,
                pageNumber: 2,
                totalRecords: 120,
                rows: [["Beta"]],
            }),
        );
    await selectProcedureAndFillCategory();
    await userEvent.click(screen.getByRole("button", { name: "Execute" }));
    await screen.findByText("Acme");

    await userEvent.click(screen.getByRole("button", { name: /Próxima|Next/ }));

    await screen.findByText("Beta");
    expect(executeMock).toHaveBeenLastCalledWith(expect.objectContaining({ pageNumber: 2 }));
});

test("Clear resets the selected procedure, results, and form", async () => {
    executeMock.mockResolvedValueOnce(sampleExecuteResponse());
    await selectProcedureAndFillCategory();
    await userEvent.click(screen.getByRole("button", { name: "Execute" }));
    await screen.findByText("Acme");

    await userEvent.click(screen.getByRole("button", { name: "Clear" }));

    expect(screen.getByText("Select a procedure to continue.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Execute" })).toBeDisabled();
});
