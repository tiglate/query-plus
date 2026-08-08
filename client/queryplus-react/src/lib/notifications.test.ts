import {
    dismissNotification,
    getNotifications,
    notify,
    subscribeNotifications,
} from "./notifications";

beforeEach(() => {
    vi.useFakeTimers();
    for (const notification of getNotifications()) dismissNotification(notification.id);
});

afterEach(() => {
    vi.useRealTimers();
});

test("notify adds a notification and notifies subscribers", () => {
    const listener = vi.fn();
    const unsubscribe = subscribeNotifications(listener);

    const id = notify("Connection lost");

    expect(listener).toHaveBeenCalledTimes(1);
    expect(getNotifications()).toEqual([{ id, message: "Connection lost", variant: "error" }]);
    unsubscribe();
});

test("notify with a duplicate message resets the existing entry instead of stacking a new one", () => {
    const first = notify("Connection lost");
    vi.advanceTimersByTime(7000);

    const second = notify("Connection lost");

    expect(second).toBe(first);
    expect(getNotifications()).toHaveLength(1);

    // still alive after the original 8s window would have elapsed, because the duplicate reset the timer
    vi.advanceTimersByTime(2000);
    expect(getNotifications()).toHaveLength(1);

    vi.advanceTimersByTime(6000);
    expect(getNotifications()).toHaveLength(0);
});

test("notifications auto-dismiss after 8 seconds", () => {
    notify("Server error");
    expect(getNotifications()).toHaveLength(1);

    vi.advanceTimersByTime(8000);

    expect(getNotifications()).toHaveLength(0);
});

test("dismissNotification removes a notification and clears its timer", () => {
    const id = notify("Server error");

    dismissNotification(id);

    expect(getNotifications()).toHaveLength(0);
});

test("unsubscribe stops further notifications", () => {
    const listener = vi.fn();
    const unsubscribe = subscribeNotifications(listener);
    unsubscribe();

    notify("Server error");

    expect(listener).not.toHaveBeenCalled();
});
