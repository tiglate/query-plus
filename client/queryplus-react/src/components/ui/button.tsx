import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import type { ButtonHTMLAttributes } from "react";
import { cn } from "@/lib/utils";

const variants = cva(
    "inline-flex items-center justify-center gap-2 rounded-md px-3 py-2 text-body font-medium transition disabled:pointer-events-none disabled:opacity-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-cyan-500",
    {
        variants: {
            variant: {
                primary: "bg-navy text-white hover:bg-navy-700",
                secondary:
                    "border border-slate-300 bg-white text-slate-700 hover:bg-slate-50 dark:border-navy-600 dark:bg-navy-800 dark:text-slate-100",
                accent: "bg-lime-500 text-navy hover:bg-lime-600",
                ghost: "text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-navy-700",
                danger: "bg-red-700 text-white hover:bg-red-800",
            },
            size: { default: "h-9", sm: "h-8 px-2 text-dense", icon: "h-8 w-8 p-0" },
        },
        defaultVariants: { variant: "primary", size: "default" },
    },
);

export interface ButtonProps
    extends ButtonHTMLAttributes<HTMLButtonElement>, VariantProps<typeof variants> {
    asChild?: boolean;
}

export function Button({
    className,
    variant,
    size,
    type = "button",
    asChild,
    ...props
}: Readonly<ButtonProps>) {
    const Component = asChild ? Slot : "button";
    return (
        <Component className={cn(variants({ variant, size }), className)} type={type} {...props} />
    );
}
