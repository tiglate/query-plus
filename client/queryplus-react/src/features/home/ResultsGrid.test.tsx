import { render, screen } from "@testing-library/react";
import type { GridColumn } from "@/api/types";
import { ResultsGrid } from "./ResultsGrid";

const sampleColumns: GridColumn[] = [
    { technicalName: "id", caption: "ID", alignment: 1, formatMask: null, visible: true },
    {
        technicalName: "name",
        caption: "Customer Name",
        alignment: 0,
        formatMask: null,
        visible: true,
    },
];

const sampleRows = [
    [1, "Acme Corp"],
    [2, "Beta LLC"],
];

test("renders column headers and grid table correctly", () => {
    render(<ResultsGrid columns={sampleColumns} rows={sampleRows} />);

    expect(screen.getByText("ID")).toBeInTheDocument();
    expect(screen.getByText("Customer Name")).toBeInTheDocument();
    expect(screen.getByRole("table")).toBeInTheDocument();
});
