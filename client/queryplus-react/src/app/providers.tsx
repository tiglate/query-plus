import { QueryClientProvider } from "@tanstack/react-query";
import type { PropsWithChildren } from "react";
import { LoadingBar } from "@/components/layout/LoadingBar";
import { NotificationCenter } from "@/components/layout/NotificationCenter";
import { queryClient } from "./queryClient";

export function AppProviders({ children }: Readonly<PropsWithChildren>) {
    return (
        <QueryClientProvider client={queryClient}>
            <LoadingBar />
            <NotificationCenter />
            {children}
        </QueryClientProvider>
    );
}
