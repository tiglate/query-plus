import type { SelectHTMLAttributes, TextareaHTMLAttributes } from "react";
import { cn } from "@/lib/utils";

export function Select({ className, ...props }: Readonly<SelectHTMLAttributes<HTMLSelectElement>>) {
    return (
        <select
            className={cn(
                "input-recessed h-9 w-full rounded-md border border-slate-300 bg-white px-2 text-body text-slate-900 outline-none focus:border-cyan-500 dark:border-navy-600 dark:bg-navy-900 dark:text-slate-100",
                className,
            )}
            {...props}
        />
    );
}

export function Textarea({
    className,
    ...props
}: Readonly<TextareaHTMLAttributes<HTMLTextAreaElement>>) {
    return (
        <textarea
            className={cn(
                "input-recessed w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-body text-slate-900 outline-none focus:border-cyan-500 dark:border-navy-600 dark:bg-navy-900 dark:text-slate-100",
                className,
            )}
            {...props}
        />
    );
}
