import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, waitFor } from "@testing-library/react";
import { LoadingBar } from "./LoadingBar";

function renderBar(queryClient: QueryClient) {
    const { container } = render(
        <QueryClientProvider client={queryClient}>
            <LoadingBar />
        </QueryClientProvider>,
    );
    return container.querySelector<HTMLElement>(".qp-loading-bar")!;
}

test("is inactive with no in-flight queries or mutations", () => {
    const bar = renderBar(new QueryClient());

    expect(bar).not.toHaveClass("is-active");
    expect(bar).toHaveAttribute("aria-hidden", "true");
});

test("activates while a query is fetching and deactivates once it settles", async () => {
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const bar = renderBar(queryClient);
    let resolveFetch!: () => void;

    void queryClient.fetchQuery({
        queryKey: ["thing"],
        queryFn: () => new Promise<string>((resolve) => (resolveFetch = () => resolve("ok"))),
    });

    await waitFor(() => expect(bar).toHaveClass("is-active"));
    expect(bar).toHaveAttribute("aria-hidden", "false");

    resolveFetch();

    await waitFor(() => expect(bar).not.toHaveClass("is-active"));
});

test("ignores queries tagged meta.skipLoadingBar", async () => {
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const bar = renderBar(queryClient);
    let resolveFetch!: () => void;

    void queryClient.fetchQuery({
        queryKey: ["background-poll"],
        meta: { skipLoadingBar: true },
        queryFn: () => new Promise<string>((resolve) => (resolveFetch = () => resolve("ok"))),
    });

    await waitFor(() => expect(queryClient.isFetching()).toBe(1));
    expect(bar).not.toHaveClass("is-active");

    resolveFetch();
});
