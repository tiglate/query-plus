import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { vi } from "vitest";
import { AppShell } from "./AppShell";
import { useUser } from "@/features/auth/useUser";
import { submitLogout } from "@/api/client";
import { THEME_KEY, FONT_SIZE_KEY } from "@/lib/preferences";

vi.mock("@/features/auth/useUser", () => ({ useUser: vi.fn() }));
vi.mock("@/api/client", () => ({ submitLogout: vi.fn().mockResolvedValue(undefined) }));
vi.mock("@/i18n", async () => {
    const actual = await vi.importActual<typeof import("@/i18n")>("@/i18n");
    return { ...actual, setLocale: vi.fn().mockResolvedValue(undefined) };
});

function mockUser(overrides: Partial<{ username: string | null; roles: string[]; isAuthenticated: boolean }>) {
    vi.mocked(useUser).mockReturnValue({
        data: { username: null, roles: [], isAuthenticated: false, ...overrides },
    } as ReturnType<typeof useUser>);
}

function renderShell() {
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    return render(
        <QueryClientProvider client={queryClient}>
            <MemoryRouter initialEntries={["/"]}>
                <AppShell />
            </MemoryRouter>
        </QueryClientProvider>,
    );
}

test("renders the primary nav and hides the Admin menu for a user without admin roles", () => {
    mockUser({ username: "alice", roles: [], isAuthenticated: true });

    renderShell();

    expect(screen.getByText("Home")).toBeInTheDocument();
    expect(screen.getByText("Support")).toBeInTheDocument();
    expect(screen.queryByText("Admin")).not.toBeInTheDocument();
});

test("shows the Admin menu trigger for a user with an admin-area role", () => {
    mockUser({ username: "admin", roles: ["ROLE_ADMIN"], isAuthenticated: true });

    renderShell();

    expect(screen.getByText("Admin")).toBeInTheDocument();
});

test("shows the username and a working logout button when authenticated", async () => {
    mockUser({ username: "alice", roles: [], isAuthenticated: true });
    renderShell();

    expect(screen.getByText("alice")).toBeInTheDocument();
    await userEvent.click(screen.getByText("Logout"));

    expect(submitLogout).toHaveBeenCalledTimes(1);
});

test("hides the username/logout controls when not authenticated", () => {
    mockUser({ username: null, roles: [], isAuthenticated: false });

    renderShell();

    expect(screen.queryByText("Logout")).not.toBeInTheDocument();
});

test("clicking font-size controls updates the stored font-size step", async () => {
    mockUser({ isAuthenticated: true });
    renderShell();

    await userEvent.click(screen.getByTitle("Increase font size"));

    expect(localStorage.getItem(FONT_SIZE_KEY)).not.toBeNull();
});

test("choosing a theme applies it to the document and persists it", async () => {
    mockUser({ isAuthenticated: true });
    renderShell();

    await userEvent.selectOptions(screen.getByLabelText("Theme"), "dark");

    expect(document.documentElement.classList.contains("dark")).toBe(true);
    expect(localStorage.getItem(THEME_KEY)).toBe("dark");
});

test("choosing a language calls setLocale with the selected locale", async () => {
    mockUser({ isAuthenticated: true });
    const { setLocale } = await import("@/i18n");
    renderShell();

    await userEvent.selectOptions(screen.getByLabelText("Language"), "en");

    expect(setLocale).toHaveBeenCalledWith("en");
});
