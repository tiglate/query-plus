import { cva, type VariantProps } from "class-variance-authority";
import type { HTMLAttributes } from "react";
import { cn } from "@/lib/utils";

const variants = cva(
    "badge-lift inline-flex items-center rounded-full px-2 py-[0.2rem] text-small-label font-semibold",
    {
        variants: {
            variant: {
                success: "bg-success-500 text-success-900",
                neutral: "bg-slate-200 text-slate-600 dark:bg-navy-600 dark:text-slate-300",
                danger: "bg-danger-700 text-white dark:bg-danger-900 dark:text-danger-100",
                warning: "bg-warning-300 text-warning-900",
            },
        },
        defaultVariants: { variant: "neutral" },
    },
);

export interface BadgeProps
    extends HTMLAttributes<HTMLSpanElement>, VariantProps<typeof variants> {}

export function Badge({ className, variant, ...props }: Readonly<BadgeProps>) {
    return <span className={cn(variants({ variant }), className)} {...props} />;
}
