import { ApiError } from "@/api/client";
import { dismissNotification, getNotifications } from "@/lib/notifications";
import { notifyOnGlobalError } from "./globalErrorNotifications";

afterEach(() => {
    for (const notification of getNotifications()) dismissNotification(notification.id);
});

test("a 500 ApiError notifies with the generic server-error message", () => {
    notifyOnGlobalError(new ApiError(500, "Internal details that should not reach the user"));

    const [notification] = getNotifications();
    expect(notification?.message).toBe(
        "Something went wrong on our end. Please try again in a moment.",
    );
});

test("a network failure (plain Error) notifies with the connection-lost message", () => {
    notifyOnGlobalError(new TypeError("Failed to fetch"));

    const [notification] = getNotifications();
    expect(notification?.message).toBe(
        "Can't reach the server. Check your connection and try again.",
    );
});

test("a 401 ApiError is ignored - apiFetch already redirects to /login", () => {
    notifyOnGlobalError(new ApiError(401, "Unauthorized"));

    expect(getNotifications()).toHaveLength(0);
});

test("a 400 validation ApiError is ignored - already surfaced inline by the form", () => {
    notifyOnGlobalError(new ApiError(400, "Validation failed"));

    expect(getNotifications()).toHaveLength(0);
});

test("a 404 ApiError is ignored", () => {
    notifyOnGlobalError(new ApiError(404, "Not found"));

    expect(getNotifications()).toHaveLength(0);
});

test("non-Error values are ignored", () => {
    notifyOnGlobalError("plain string rejection");
    notifyOnGlobalError(undefined);

    expect(getNotifications()).toHaveLength(0);
});
