import type { PropsWithChildren, ReactNode } from "react";
import { Card, CardBody, CardHeader } from "./card";

interface PageHeaderProps extends PropsWithChildren {
    title: string;
    icon?: ReactNode;
    actions?: ReactNode;
}

interface FieldProps extends PropsWithChildren {
    label: string;
    required?: boolean;
    error?: string;
}

interface SectionProps extends PropsWithChildren {
    title: string;
    actions?: ReactNode;
}

export function PageHeader({ title, icon, actions }: PageHeaderProps) {
    return (
        <Card>
            <CardHeader>
                <h1 className="flex items-center gap-2 text-page-title font-semibold text-navy dark:text-slate-100">
                    {icon}
                    {title}
                </h1>
                <div className="flex flex-wrap gap-2">{actions}</div>
            </CardHeader>
        </Card>
    );
}

export function Field({ label, required, error, children }: FieldProps) {
    return (
        <label className="block min-w-0 text-small-label font-medium text-slate-700 dark:text-slate-200">
            <span className={required ? "after:ml-1 after:text-red-600 after:content-['*']" : ""}>
                {label}
            </span>
            <span className="mt-1 block">{children}</span>
            {error && (
                <span className="mt-1 block text-small-label text-red-700 dark:text-red-400">
                    {error}
                </span>
            )}
        </label>
    );
}

export function Section({ title, actions, children }: SectionProps) {
    return (
        <Card>
            <CardHeader>
                <h2 className="text-card-title font-semibold">{title}</h2>
                {actions}
            </CardHeader>
            <CardBody>{children}</CardBody>
        </Card>
    );
}
