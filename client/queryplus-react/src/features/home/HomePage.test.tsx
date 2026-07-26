import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ExecuteResponse } from "@/api/types";
import { Pager } from "@/components/ui/pager";
import { isExportEligible } from "./HomePage";

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
