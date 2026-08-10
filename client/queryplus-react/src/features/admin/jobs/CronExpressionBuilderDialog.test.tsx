import { fireEvent, render, screen } from "@testing-library/react";
import { vi } from "vitest";
import { CronExpressionBuilderDialog } from "./CronExpressionBuilderDialog";

function renderDialog({
    initialValue = "",
    onApply = vi.fn<(expression: string) => void>(),
    onOpenChange = vi.fn<(open: boolean) => void>(),
}: {
    initialValue?: string;
    onApply?: ReturnType<typeof vi.fn<(expression: string) => void>>;
    onOpenChange?: ReturnType<typeof vi.fn<(open: boolean) => void>>;
} = {}) {
    render(
        <CronExpressionBuilderDialog
            open
            initialValue={initialValue}
            onOpenChange={onOpenChange}
            onApply={onApply}
        />,
    );
    return { onApply, onOpenChange };
}

describe("CronExpressionBuilderDialog", () => {
    it("produces the expected 5-field string for Daily mode at a given time", () => {
        const { onApply, onOpenChange } = renderDialog();

        fireEvent.click(screen.getByRole("radio", { name: "Daily" }));
        const timeInput = screen.getByLabelText("Time");
        fireEvent.change(timeInput, { target: { value: "07:30" } });

        fireEvent.click(screen.getByRole("button", { name: "Apply" }));

        expect(onApply).toHaveBeenCalledWith("30 7 * * *");
        expect(onOpenChange).toHaveBeenCalledWith(false);
    });

    it("requires at least one day selected in Weekly mode before Apply is enabled", () => {
        renderDialog();

        fireEvent.click(screen.getByRole("radio", { name: "Weekly" }));
        const applyButton = screen.getByRole("button", { name: "Apply" });
        expect(applyButton).toBeDisabled();

        fireEvent.click(screen.getByLabelText("Monday"));
        expect(applyButton).toBeEnabled();
    });

    it("computes the weekly expression from the selected days and time", () => {
        const { onApply } = renderDialog();

        fireEvent.click(screen.getByRole("radio", { name: "Weekly" }));
        fireEvent.change(screen.getByLabelText("Time"), { target: { value: "09:15" } });
        fireEvent.click(screen.getByLabelText("Monday"));
        fireEvent.click(screen.getByLabelText("Friday"));

        fireEvent.click(screen.getByRole("button", { name: "Apply" }));

        expect(onApply).toHaveBeenCalledWith("15 9 * * 1,5");
    });

    it("round-trips a 5-part initialValue into the Custom mode inputs", () => {
        renderDialog({ initialValue: "15 3 1 * 2" });

        expect(screen.getByLabelText("Minute")).toHaveValue("15");
        expect(screen.getByLabelText("Hour")).toHaveValue("3");
        expect(screen.getByLabelText("Day of month")).toHaveValue("1");
        expect(screen.getByLabelText("Month")).toHaveValue("*");
        expect(screen.getByLabelText("Day of week")).toHaveValue("2");
    });

    it("defaults Custom mode fields to asterisks when initialValue does not split into 5 parts", () => {
        renderDialog({ initialValue: "not a cron expression at all here" });

        expect(screen.getByLabelText("Minute")).toHaveValue("*");
        expect(screen.getByLabelText("Hour")).toHaveValue("*");
        expect(screen.getByLabelText("Day of month")).toHaveValue("*");
        expect(screen.getByLabelText("Month")).toHaveValue("*");
        expect(screen.getByLabelText("Day of week")).toHaveValue("*");
    });

    it("calls onOpenChange(false) without applying when Cancel is clicked", () => {
        const { onApply, onOpenChange } = renderDialog();

        fireEvent.click(screen.getByRole("button", { name: "Cancel" }));

        expect(onOpenChange).toHaveBeenCalledWith(false);
        expect(onApply).not.toHaveBeenCalled();
    });
});
