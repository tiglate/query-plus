import type { InputHTMLAttributes } from "react";
import { cn } from "@/lib/utils";

export function Switch({ className, ...props }: Readonly<InputHTMLAttributes<HTMLInputElement>>) {
    return (
        <label className="inline-flex cursor-pointer items-center has-[:disabled]:cursor-not-allowed">
            <input type="checkbox" role="switch" className="peer sr-only" {...props} />
            <span
                className={cn(
                    "relative h-5 w-9 shrink-0 rounded-full bg-slate-300 transition-colors after:absolute after:left-0.5 after:top-0.5 after:h-4 after:w-4 after:rounded-full after:bg-white after:transition-transform after:content-[''] peer-checked:bg-cyan-500 peer-checked:after:translate-x-4 peer-focus-visible:outline peer-focus-visible:outline-2 peer-focus-visible:outline-offset-2 peer-focus-visible:outline-cyan-500 peer-disabled:opacity-50 dark:bg-navy-600",
                    className,
                )}
            />
        </label>
    );
}
