import type { HTMLAttributes } from "react";
import { cn } from "@/lib/utils";

export function Card({ className, ...props }: Readonly<HTMLAttributes<HTMLDivElement>>) {
    return (
        <section
            className={cn(
                "rounded-lg border border-slate-200 bg-white shadow-sm dark:border-navy-600 dark:bg-navy-800",
                className,
            )}
            {...props}
        />
    );
}

export function CardHeader({ className, ...props }: Readonly<HTMLAttributes<HTMLDivElement>>) {
    return (
        <div
            className={cn(
                "flex min-h-12 items-center justify-between gap-3 border-b border-slate-200 px-4 py-2 dark:border-navy-600",
                className,
            )}
            {...props}
        />
    );
}

export function CardBody({ className, ...props }: Readonly<HTMLAttributes<HTMLDivElement>>) {
    return <div className={cn("p-4", className)} {...props} />;
}
