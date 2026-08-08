import { useIsFetching, useIsMutating } from "@tanstack/react-query";

/**
 * Slim top-of-viewport activity strip driven by every in-flight query/mutation, so any part
 * of the app that talks to the server shows visible activity on a slow day - independent of
 * whether the triggering element has its own local spinner. Ported from the pre-React MVC
 * app's htmx-based LoadingBarService/qp-loading-bar.
 *
 * Queries tagged `meta: { skipLoadingBar: true }` opt out (e.g. the export-status background
 * poll, which already has its own local pulse and would otherwise flicker the bar every
 * couple seconds while a job is pending).
 */
export function LoadingBar() {
    const fetching = useIsFetching({ predicate: (query) => !query.meta?.skipLoadingBar });
    const mutating = useIsMutating();
    const active = fetching + mutating > 0;

    return (
        <div
            className={`qp-loading-bar${active ? " is-active" : ""}`}
            role="progressbar"
            aria-hidden={!active}
        />
    );
}
