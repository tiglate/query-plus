import type { InputHTMLAttributes } from "react";
import { cn } from "@/lib/utils";

export function Input({ className, ...props }: Readonly<InputHTMLAttributes<HTMLInputElement>>) {
    return (
        <input
            className={cn(
                "input-recessed h-9 w-full rounded-md border border-slate-300 bg-white px-3 text-body text-slate-900 outline-none focus:border-cyan-500 disabled:opacity-60 dark:border-navy-600 dark:bg-navy-900 dark:text-slate-100",
                className,
            )}
            {...props}
        />
    );
}
