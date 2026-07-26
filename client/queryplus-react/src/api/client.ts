export interface ApiErrorBody {
    title?: string;
    detail?: string;
    message?: string;
    errors?: Record<string, string[]>;
}

export class ApiError extends Error {
    constructor(
        public readonly status: number,
        message: string,
        public readonly details?: ApiErrorBody | string,
    ) {
        super(message);
        this.name = "ApiError";
    }
}

let csrfToken: string | null = null;
let csrfPromise: Promise<string> | null = null;
let unauthorizedRedirect: (() => void) | null = null;

function isUnsafe(method: string): boolean {
    return !["GET", "HEAD", "OPTIONS"].includes(method.toUpperCase());
}

async function parseResponse(response: Response): Promise<unknown> {
    if (response.status === 204) return undefined;
    const type = response.headers.get("content-type") ?? "";
    if (type.includes("json")) return response.json();
    const text = await response.text();
    return text || undefined;
}

export async function getCsrfToken(): Promise<string> {
    if (csrfToken) {
        return csrfToken;
    }
    csrfPromise ??= fetch("/api/auth/csrf", { credentials: "include" })
        .then(async (response) => {
            if (!response.ok) {
                throw new ApiError(response.status, "Unable to obtain CSRF token");
            }
            const body = (await response.json()) as { token: string };
            csrfToken = body.token;
            return body.token;
        })
        .finally(() => {
            csrfPromise = null;
        });
    return csrfPromise;
}

export function redirectToLogin(): void {
    if (unauthorizedRedirect) {
        unauthorizedRedirect();
        return;
    }
    const returnUrl = `${window.location.pathname}${window.location.search}${window.location.hash}`;
    window.location.assign(`/login?returnUrl=${encodeURIComponent(returnUrl)}`);
}

export function setUnauthorizedRedirect(handler: (() => void) | null): void {
    unauthorizedRedirect = handler;
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
    const method = (init.method ?? "GET").toUpperCase();
    const headers = new Headers(init.headers);
    if (init.body && !(init.body instanceof FormData) && !headers.has("Content-Type")) {
        headers.set("Content-Type", "application/json");
    }
    if (isUnsafe(method)) headers.set("X-CSRF-TOKEN", await getCsrfToken());

    const response = await fetch(path, { ...init, method, headers, credentials: "include" });
    if (response.status === 401) {
        redirectToLogin();
        throw new ApiError(401, "Unauthorized");
    }

    const body = await parseResponse(response);
    if (!response.ok) {
        const problem = body as ApiErrorBody | string | undefined;
        const message =
            typeof problem === "string"
                ? problem
                : (problem?.detail ?? problem?.message ?? problem?.title ?? response.statusText);
        throw new ApiError(response.status, message || "Request failed", problem);
    }
    return body as T;
}

export async function submitLogout(): Promise<void> {
    const token = await getCsrfToken();
    const form = document.createElement("form");
    form.method = "post";
    form.action = "/api/auth/logout";
    const input = document.createElement("input");
    input.type = "hidden";
    input.name = "__RequestVerificationToken";
    input.value = token;
    form.append(input);
    document.body.append(form);
    form.submit();
}

export function resetCsrfToken(): void {
    csrfToken = null;
    csrfPromise = null;
}
