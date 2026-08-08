import { clsx, type ClassValue } from "clsx";
import { extendTailwindMerge } from "tailwind-merge";

// Tailwind v4's CSS-defined `--text-*` theme tokens (see globals.css `@theme`) aren't visible to
// tailwind-merge's default config, which then misclassifies e.g. `text-small-label` as a
// text-color utility and drops it when merged alongside a real color class like `text-slate-700`.
const twMerge = extendTailwindMerge({
    extend: {
        classGroups: {
            "font-size": [
                {
                    text: [
                        "caption",
                        "small-label",
                        "dense",
                        "body",
                        "card-title",
                        "page-title",
                        "display-sm",
                        "display-lg",
                    ],
                },
            ],
        },
    },
});

export function cn(...inputs: ClassValue[]): string {
    return twMerge(clsx(inputs));
}

export function formatTemplate(template: string, ...values: Array<string | number>): string {
    let result = template;
    values.forEach((value, index) => {
        result = result.replace(`{${index}}`, String(value));
    });
    return result;
}
