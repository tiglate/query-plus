import { apiFetch, resetCsrfToken, setUnauthorizedRedirect } from "./client";

beforeEach(() => {
    resetCsrfToken();
    setUnauthorizedRedirect(null);
});

test("unsafe requests cache CSRF token and include credentials", async () => {
    const fetchMock = vi
        .spyOn(globalThis, "fetch")
        .mockResolvedValueOnce(
            new Response(JSON.stringify({ token: "csrf-token" }), {
                status: 200,
                headers: { "content-type": "application/json" },
            }),
        )
        .mockResolvedValueOnce(
            new Response(JSON.stringify({ ok: true }), {
                status: 200,
                headers: { "content-type": "application/json" },
            }),
        )
        .mockResolvedValueOnce(
            new Response(JSON.stringify({ ok: true }), {
                status: 200,
                headers: { "content-type": "application/json" },
            }),
        );

    await apiFetch("/api/categories", { method: "POST", body: "{}" });
    await apiFetch("/api/categories/1", { method: "DELETE" });

    expect(fetchMock).toHaveBeenCalledTimes(3);
    expect(fetchMock.mock.calls[0]?.[0]).toBe("/api/auth/csrf");
    const firstInit = fetchMock.mock.calls[1]?.[1];
    expect(firstInit?.credentials).toBe("include");
    expect(new Headers(firstInit?.headers).get("X-CSRF-TOKEN")).toBe("csrf-token");
});

test("401 invokes login redirect and throws typed error", async () => {
    const redirect = vi.fn();
    setUnauthorizedRedirect(redirect);
    vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response(null, { status: 401 }));

    await expect(apiFetch("/api/auth/user")).rejects.toMatchObject({ status: 401 });
    expect(redirect).toHaveBeenCalledOnce();
});
