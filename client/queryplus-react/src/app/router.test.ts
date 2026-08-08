import { vi } from "vitest";
import { authQuery } from "@/api/queries";
import { EXECUTION_LOG_ROLES } from "@/features/auth/roles";
import { authLoader, requireAnyRole } from "./router";
import { queryClient } from "./queryClient";

function pendingWithin(ms: number, promise: Promise<unknown>): Promise<"pending" | "settled"> {
    return Promise.race([
        promise.then(() => "settled" as const),
        new Promise<"pending">((resolve) => setTimeout(() => resolve("pending"), ms)),
    ]);
}

afterEach(() => {
    queryClient.clear();
    vi.restoreAllMocks();
});

test("authLoader never resolves for an unauthenticated user, and redirects instead", async () => {
    vi.spyOn(queryClient, "ensureQueryData").mockImplementation((options) => {
        if (options === authQuery) {
            return Promise.resolve({
                isAuthenticated: false,
                username: "",
                roles: [],
            }) as never;
        }
        throw new Error("unexpected query");
    });
    const assign = vi.fn();
    vi.stubGlobal("location", { ...window.location, assign });

    const request = new Request("http://localhost/admin/procedures?x=1#y");
    const outcome = await pendingWithin(50, authLoader({ request }));

    expect(outcome).toBe("pending");
    expect(assign).toHaveBeenCalledWith(
        `/login?returnUrl=${encodeURIComponent("/admin/procedures?x=1#y")}`,
    );
});

test("authLoader resolves immediately for an authenticated user, without redirecting", async () => {
    const user = { isAuthenticated: true, username: "demo", roles: ["ROLE_QUERY_EXEC"] };
    vi.spyOn(queryClient, "ensureQueryData").mockImplementation((options) => {
        if (options === authQuery) return Promise.resolve(user) as never;
        throw new Error("unexpected query");
    });
    const assign = vi.fn();
    vi.stubGlobal("location", { ...window.location, assign });

    const result = await authLoader({ request: new Request("http://localhost/") });

    expect(result).toEqual(user);
    expect(assign).not.toHaveBeenCalled();
});

test("requireAnyRole throws a 403 Response for a user missing every required role", async () => {
    vi.spyOn(queryClient, "ensureQueryData").mockImplementation((options) => {
        if (options === authQuery) {
            return Promise.resolve({
                isAuthenticated: true,
                username: "demo",
                roles: ["ROLE_QUERY_EXEC"],
            }) as never;
        }
        throw new Error("unexpected query");
    });

    const loader = requireAnyRole(EXECUTION_LOG_ROLES);
    await expect(loader()).rejects.toMatchObject({ status: 403 });
});

test("requireAnyRole resolves for a user holding one of the required roles", async () => {
    vi.spyOn(queryClient, "ensureQueryData").mockImplementation((options) => {
        if (options === authQuery) {
            return Promise.resolve({
                isAuthenticated: true,
                username: "admin",
                roles: ["ROLE_ADMIN"],
            }) as never;
        }
        throw new Error("unexpected query");
    });

    const loader = requireAnyRole(EXECUTION_LOG_ROLES);
    await expect(loader()).resolves.toBeNull();
});
