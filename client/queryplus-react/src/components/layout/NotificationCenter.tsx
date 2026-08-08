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
                    className="flex w-full max-w-md items-start gap-2 rounded-md border border-red-300 bg-red-50 p-3 text-body text-red-800 shadow-lg dark:border-red-900 dark:bg-red-950 dark:text-red-200"
                >
                    <CircleAlert className="mt-0.5 h-4 w-4 shrink-0" />
                    <p className="flex-1">{notification.message}</p>
                    <button
                        type="button"
                        onClick={() => dismissNotification(notification.id)}
                        aria-label={t("Notification_Dismiss")}
                        className="shrink-0 rounded p-0.5 hover:bg-red-100 dark:hover:bg-red-900"
                    >
                        <X className="h-4 w-4" />
                    </button>
                </div>
            ))}
        </div>
    );
}
