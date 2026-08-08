import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { StrictMode } from "react";
import { MaximizeButton, RESULTS_MAX_STORAGE_KEY, useResultsMaximize } from "./ResultsMaximize";

function Harness() {
    const state = useResultsMaximize();
    return (
        <>
            <output>{state.maximized ? "max" : "normal"}</output>
            <MaximizeButton maximized={state.maximized} onToggle={state.toggle} />
        </>
    );
}

test("one click toggles maximize once under strict mode and persists it", async () => {
    render(
        <StrictMode>
            <Harness />
        </StrictMode>,
    );
    await userEvent.click(screen.getByRole("button", { name: /Maximizar|Maximize/ }));
    expect(screen.getByText("max")).toBeInTheDocument();
    expect(sessionStorage.getItem(RESULTS_MAX_STORAGE_KEY)).toBe("1");
});
