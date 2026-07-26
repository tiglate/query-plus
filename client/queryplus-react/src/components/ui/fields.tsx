import type { SelectHTMLAttributes, TextareaHTMLAttributes } from "react";
import { cn } from "@/lib/utils";

export function Select({ className, ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
    return (
        <select
            className={cn(
                "h-9 w-full rounded-md border border-slate-300 bg-white px-2 text-body text-slate-900 outline-none focus:border-cyan-500 focus:ring-2 focus:ring-cyan-500/20 dark:border-navy-600 dark:bg-navy-900 dark:text-slate-100",
                className,
            )}
            {...props}
        />
    );
}

export function Textarea({ className, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
    return (
        <textarea
            className={cn(
                "w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-body text-slate-900 outline-none focus:border-cyan-500 focus:ring-2 focus:ring-cyan-500/20 dark:border-navy-600 dark:bg-navy-900 dark:text-slate-100",
                className,
            )}
            {...props}
        />
    );
}
