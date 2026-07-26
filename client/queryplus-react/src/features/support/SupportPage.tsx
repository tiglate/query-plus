import { Clock, Headphones, LifeBuoy, Mail, Phone } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Card, CardBody, CardHeader } from "@/components/ui/card";

export function SupportPage() {
    const { t } = useTranslation();
    const contacts = [
        { label: t("Support_Helpdesk"), value: "helpdesk.example.com", icon: Headphones },
        { label: t("Support_Phone"), value: "+55 (11) 0000-0000", icon: Phone },
        { label: t("Support_Email"), value: "support@queryplus.local", icon: Mail },
        { label: t("Support_Hours"), value: "Seg–Sex · 09:00–18:00 (BRT)", icon: Clock },
    ];
    return (
        <div className="space-y-4 p-4">
            <Card>
                <CardHeader>
                    <h1 className="flex items-center gap-2 text-page-title font-semibold">
                        <LifeBuoy className="h-4 w-4 text-cyan-500" />
                        {t("Support_Title")}
                    </h1>
                </CardHeader>
            </Card>
            <Card>
                <CardBody>
                    <p className="mb-5 text-body text-slate-600 dark:text-slate-300">
                        {t("Support_Body")}
                    </p>
                    <dl className="grid gap-3 sm:grid-cols-2">
                        {contacts.map(({ label, value, icon: Icon }) => (
                            <div
                                key={label}
                                className="rounded-md border border-slate-200 bg-slate-50 p-4 dark:border-navy-600 dark:bg-navy-900"
                            >
                                <dt className="flex items-center gap-2 text-small-label font-semibold">
                                    <Icon className="h-4 w-4 text-cyan-500" />
                                    {label}
                                </dt>
                                <dd className="mt-2 text-body">{value}</dd>
                            </div>
                        ))}
                    </dl>
                </CardBody>
            </Card>
        </div>
    );
}
