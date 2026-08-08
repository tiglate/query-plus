import { ApiError } from "@/api/client";
import i18n from "@/i18n";
import { notify } from "@/lib/notifications";

/**
 * Surfaces connection/server failures that would otherwise be invisible: a plain useQuery
 * has no built-in error UI, so a page just keeps showing stale/empty data with no explanation
 * once the server stops responding mid-session. Ordinary 4xx (validation, forbidden, not
 * found) are left alone - those are already rendered inline by the page/form that owns them,
 * and are not "something went wrong on the server" in the sense the user needs to be warned
 * about. Never surface `error.message`/`details` here - those can carry response bodies that
 * are fine as inline form context but not as a broadcast toast.
 */
export function notifyOnGlobalError(error: unknown): void {
    if (error instanceof ApiError) {
        if (error.status === 401) return; // apiFetch already redirects to /login
        if (error.status >= 500) notify(i18n.t("Notification_ServerError"));
        return;
    }
    // fetch() rejects with a plain (non-ApiError) Error when the request never reaches the
    // server at all - refused connection, DNS failure, offline, etc.
    if (error instanceof Error) notify(i18n.t("Notification_ConnectionLost"));
}
