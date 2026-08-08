export type NotificationVariant = "error";

export interface AppNotification {
    id: number;
    message: string;
    variant: NotificationVariant;
}

const AUTO_DISMISS_MS = 8000;

let notifications: AppNotification[] = [];
let nextId = 1;
const listeners = new Set<() => void>();
const timers = new Map<number, ReturnType<typeof setTimeout>>();

function emit(): void {
    for (const listener of listeners) listener();
}

function clearTimer(id: number): void {
    const timer = timers.get(id);
    if (timer !== undefined) {
        clearTimeout(timer);
        timers.delete(id);
    }
}

function scheduleDismiss(id: number): void {
    clearTimer(id);
    timers.set(
        id,
        setTimeout(() => dismissNotification(id), AUTO_DISMISS_MS),
    );
}

/**
 * Pushes a global notification, or - if one with the same message is already showing -
 * resets its auto-dismiss timer instead of stacking a duplicate. Without this, a background
 * poll hitting the same failure (e.g. export-status while the server is down) would spawn a
 * fresh toast every retry forever instead of just keeping the one already on screen alive.
 */
export function notify(message: string, variant: NotificationVariant = "error"): number {
    const existing = notifications.find((n) => n.variant === variant && n.message === message);
    if (existing) {
        scheduleDismiss(existing.id);
        return existing.id;
    }
    const id = nextId++;
    notifications = [...notifications, { id, message, variant }];
    scheduleDismiss(id);
    emit();
    return id;
}

export function dismissNotification(id: number): void {
    clearTimer(id);
    if (!notifications.some((n) => n.id === id)) return;
    notifications = notifications.filter((n) => n.id !== id);
    emit();
}

export function getNotifications(): AppNotification[] {
    return notifications;
}

export function subscribeNotifications(listener: () => void): () => void {
    listeners.add(listener);
    return () => listeners.delete(listener);
}
