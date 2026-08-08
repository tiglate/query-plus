import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import type { ButtonHTMLAttributes } from "react";
import { cn } from "@/lib/utils";

const variants = cva(
    "inline-flex items-center justify-center gap-2 px-3 py-2 text-body font-medium outline-none disabled:pointer-events-none disabled:opacity-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-cyan-500",
    {
        variants: {
            variant: {
                primary: "btn-bevel btn-bevel-primary",
                secondary: "btn-bevel btn-bevel-secondary",
                accent: "btn-bevel btn-bevel-accent",
                ghost: "rounded-md text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-navy-700",
                danger: "btn-bevel btn-bevel-danger",
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
