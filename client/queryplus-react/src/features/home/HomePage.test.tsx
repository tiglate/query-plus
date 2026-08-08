import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useForm } from "react-hook-form";
import type { ExecuteResponse } from "@/api/types";
import { Pager } from "@/components/ui/pager";
import {
    ExportButton,
    isExportEligible,
    resultSignature,
    type ParameterFormValues,
} from "./HomePage";

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
