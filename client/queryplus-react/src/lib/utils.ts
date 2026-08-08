import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

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
