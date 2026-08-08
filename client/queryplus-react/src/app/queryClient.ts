import { MutationCache, QueryCache, QueryClient } from "@tanstack/react-query";
import { notifyOnGlobalError } from "./globalErrorNotifications";

export const queryClient = new QueryClient({
    queryCache: new QueryCache({ onError: notifyOnGlobalError }),
    mutationCache: new MutationCache({ onError: notifyOnGlobalError }),
    defaultOptions: {
        queries: { staleTime: 30_000, retry: 1, refetchOnWindowFocus: false },
        mutations: { retry: 0 },
    },
});
