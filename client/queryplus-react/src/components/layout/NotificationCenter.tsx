import { CircleAlert, X } from "lucide-react";
import { useSyncExternalStore } from "react";
import { useTranslation } from "react-i18next";
import { dismissNotification, getNotifications, subscribeNotifications } from "@/lib/notifications";

export function NotificationCenter() {
    const notifications = useSyncExternalStore(subscribeNotifications, getNotifications);
    const { t } = useTranslation();

    if (notifications.length === 0) return null;

    return (
        <div
            className="fixed inset-x-0 top-2 z-[80] flex flex-col items-center gap-2 px-3"
            aria-live="assertive"
            role="alert"
        >
            {notifications.map((notification) => (
                <div
                    key={notification.id}
                    className="flex w-full max-w-md items-start gap-2 rounded-md border border-danger-line bg-danger-subtle p-3 text-body text-danger shadow-lg"
                >
                    <CircleAlert className="mt-0.5 h-4 w-4 shrink-0" />
                    <p className="flex-1">{notification.message}</p>
                    <button
                        type="button"
                        onClick={() => dismissNotification(notification.id)}
                        aria-label={t("Notification_Dismiss")}
                        className="shrink-0 rounded p-0.5 hover:bg-danger-100 dark:hover:bg-danger-800"
                    >
                        <X className="h-4 w-4" />
                    </button>
                </div>
            ))}
        </div>
    );
}
