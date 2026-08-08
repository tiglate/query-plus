import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { vi } from "vitest";
import { NotFoundPage, RouteErrorPage } from "./errors";

vi.mock("react-router-dom", async () => {
    const actual = await vi.importActual<typeof import("react-router-dom")>("react-router-dom");
    return {
        ...actual,
        useRouteError: vi.fn(),
        isRouteErrorResponse: vi.fn(),
    };
});

function renderWithRouter(component: React.ReactNode) {
    return render(<MemoryRouter>{component}</MemoryRouter>);
}

test("NotFoundPage shows the not-found copy and a link home", () => {
    renderWithRouter(<NotFoundPage />);

    expect(screen.getByText("Oops! Page went missing")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Go home/ })).toHaveAttribute("href", "/");
});

test("RouteErrorPage shows the status code for a route error response", async () => {
    const { useRouteError, isRouteErrorResponse } = await import("react-router-dom");
    vi.mocked(useRouteError).mockReturnValue({ status: 503, statusText: "Unavailable" });
    vi.mocked(isRouteErrorResponse).mockReturnValue(true);

    renderWithRouter(<RouteErrorPage />);

    expect(screen.getByText("Error 503: the server stubbed its toe")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Go home/ })).toHaveAttribute("href", "/");
    expect(screen.getByRole("link", { name: /Contact support/ })).toHaveAttribute("href", "/support");
});

test("RouteErrorPage falls back to status 500 and the thrown error's message for a generic error", async () => {
    const { useRouteError, isRouteErrorResponse } = await import("react-router-dom");
    vi.mocked(useRouteError).mockReturnValue(new Error("boom"));
    vi.mocked(isRouteErrorResponse).mockReturnValue(false);

    renderWithRouter(<RouteErrorPage />);

    expect(screen.getByText("Error 500: the server stubbed its toe")).toBeInTheDocument();
    expect(screen.getByText("boom")).toBeInTheDocument();
});
