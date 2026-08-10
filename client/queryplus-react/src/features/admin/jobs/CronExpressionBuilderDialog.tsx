import * as Dialog from "@radix-ui/react-dialog";
import { X } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/page";
import { Input } from "@/components/ui/input";

export interface CronExpressionBuilderDialogProps {
    open: boolean;
    initialValue: string;
    onOpenChange: (open: boolean) => void;
    onApply: (expression: string) => void;
}

const MODES = ["minutes", "hours", "daily", "weekly", "monthly", "custom"] as const;
type Mode = (typeof MODES)[number];

const MODE_LABEL_KEYS: Record<Mode, string> = {
    minutes: "CronBuilder_Mode_Minutes",
    hours: "CronBuilder_Mode_Hours",
    daily: "CronBuilder_Mode_Daily",
    weekly: "CronBuilder_Mode_Weekly",
    monthly: "CronBuilder_Mode_Monthly",
    custom: "CronBuilder_Mode_Custom",
};

const WEEKDAYS = [0, 1, 2, 3, 4, 5, 6];

interface CronResult {
    expression: string;
    preview: string;
}

function parseIntInRange(raw: string, min: number, max: number): number | null {
    const trimmed = raw.trim();
    if (!/^\d+$/.test(trimmed)) return null;
    const value = Number(trimmed);
    if (value < min || value > max) return null;
    return value;
}

function parseTime(raw: string): { h: number; m: number } | null {
    const parts = raw.split(":");
    if (parts.length < 2) return null;
    const h = Number(parts[0]);
    const m = Number(parts[1]);
    if (!Number.isInteger(h) || !Number.isInteger(m)) return null;
    if (h < 0 || h > 23 || m < 0 || m > 59) return null;
    return { h, m };
}

export function CronExpressionBuilderDialog({
    open,
    initialValue,
    onOpenChange,
    onApply,
}: Readonly<CronExpressionBuilderDialogProps>) {
    const { t } = useTranslation();

    const [mode, setMode] = useState<Mode>("custom");
    const [minutesN, setMinutesN] = useState("5");
    const [hoursN, setHoursN] = useState("1");
    const [hoursM, setHoursM] = useState("0");
    const [dailyTime, setDailyTime] = useState("00:00");
    const [weeklyTime, setWeeklyTime] = useState("00:00");
    const [weeklyDays, setWeeklyDays] = useState<number[]>([]);
    const [monthlyDay, setMonthlyDay] = useState("1");
    const [monthlyTime, setMonthlyTime] = useState("00:00");
    const [customMinute, setCustomMinute] = useState("*");
    const [customHour, setCustomHour] = useState("*");
    const [customDom, setCustomDom] = useState("*");
    const [customMonth, setCustomMonth] = useState("*");
    const [customDow, setCustomDow] = useState("*");

    useEffect(() => {
        if (!open) return;
        setMode("custom");
        setMinutesN("5");
        setHoursN("1");
        setHoursM("0");
        setDailyTime("00:00");
        setWeeklyTime("00:00");
        setWeeklyDays([]);
        setMonthlyDay("1");
        setMonthlyTime("00:00");

        const parts = initialValue
            .trim()
            .split(/\s+/)
            .filter((part) => part !== "");
        if (parts.length === 5) {
            setCustomMinute(parts[0] ?? "*");
            setCustomHour(parts[1] ?? "*");
            setCustomDom(parts[2] ?? "*");
            setCustomMonth(parts[3] ?? "*");
            setCustomDow(parts[4] ?? "*");
        } else {
            setCustomMinute("*");
            setCustomHour("*");
            setCustomDom("*");
            setCustomMonth("*");
            setCustomDow("*");
        }
    }, [open, initialValue]);

    const result: CronResult | null = useMemo(() => {
        switch (mode) {
            case "minutes": {
                const n = parseIntInRange(minutesN, 1, 59);
                if (n === null) return null;
                return {
                    expression: `*/${n} * * * *`,
                    preview: t("CronBuilder_Preview_Minutes", { n }),
                };
            }
            case "hours": {
                const n = parseIntInRange(hoursN, 1, 23);
                const m = parseIntInRange(hoursM, 0, 59);
                if (n === null || m === null) return null;
                return {
                    expression: `${m} */${n} * * *`,
                    preview: t("CronBuilder_Preview_Hours", { n, m }),
                };
            }
            case "daily": {
                const time = parseTime(dailyTime);
                if (!time) return null;
                return {
                    expression: `${time.m} ${time.h} * * *`,
                    preview: t("CronBuilder_Preview_Daily", { time: dailyTime }),
                };
            }
            case "weekly": {
                const time = parseTime(weeklyTime);
                if (!time || weeklyDays.length === 0) return null;
                const sorted = [...weeklyDays].sort((a, b) => a - b);
                const dayNames = sorted.map((day) => t(`CronBuilder_Weekday_${day}`)).join(", ");
                return {
                    expression: `${time.m} ${time.h} * * ${sorted.join(",")}`,
                    preview: t("CronBuilder_Preview_Weekly", { days: dayNames, time: weeklyTime }),
                };
            }
            case "monthly": {
                const day = parseIntInRange(monthlyDay, 1, 28);
                const time = parseTime(monthlyTime);
                if (day === null || !time) return null;
                return {
                    expression: `${time.m} ${time.h} ${day} * *`,
                    preview: t("CronBuilder_Preview_Monthly", { day, time: monthlyTime }),
                };
            }
            case "custom": {
                const fields = [customMinute, customHour, customDom, customMonth, customDow].map(
                    (field) => field.trim(),
                );
                if (fields.some((field) => field === "")) return null;
                return {
                    expression: fields.join(" "),
                    preview: t("CronBuilder_Preview_Custom"),
                };
            }
            default:
                return null;
        }
    }, [
        mode,
        minutesN,
        hoursN,
        hoursM,
        dailyTime,
        weeklyTime,
        weeklyDays,
        monthlyDay,
        monthlyTime,
        customMinute,
        customHour,
        customDom,
        customMonth,
        customDow,
        t,
    ]);

    function handleApply() {
        if (!result) return;
        onApply(result.expression);
        onOpenChange(false);
    }

    return (
        <Dialog.Root open={open} onOpenChange={onOpenChange}>
            <Dialog.Portal>
                <Dialog.Overlay className="fixed inset-0 z-50 bg-black/45" />
                <Dialog.Content className="fixed left-1/2 top-1/2 z-50 flex max-h-[85vh] w-[min(34rem,calc(100%-2rem))] -translate-x-1/2 -translate-y-1/2 flex-col rounded-lg bg-white p-5 shadow-xl dark:bg-navy-800">
                    <div className="flex items-center justify-between">
                        <Dialog.Title className="text-card-title font-semibold">
                            {t("CronBuilder_Title")}
                        </Dialog.Title>
                        <Dialog.Close asChild>
                            <Button type="button" variant="ghost" size="icon">
                                <X className="h-4 w-4" />
                            </Button>
                        </Dialog.Close>
                    </div>

                    <div className="mt-4 flex-1 space-y-4 overflow-y-auto">
                        <div
                            role="radiogroup"
                            aria-label={t("CronBuilder_Title")}
                            className="flex flex-wrap gap-3"
                        >
                            {MODES.map((m) => (
                                <label
                                    key={m}
                                    className="flex items-center gap-1.5 text-body text-slate-700 dark:text-slate-200"
                                >
                                    <input
                                        type="radio"
                                        name="cron-builder-mode"
                                        value={m}
                                        checked={mode === m}
                                        onChange={() => setMode(m)}
                                        className="h-4 w-4"
                                    />
                                    {t(MODE_LABEL_KEYS[m])}
                                </label>
                            ))}
                        </div>

                        {mode === "minutes" && (
                            <Field label={t("CronBuilder_Minutes_N")}>
                                <Input
                                    type="number"
                                    min={1}
                                    max={59}
                                    value={minutesN}
                                    onChange={(event) => setMinutesN(event.target.value)}
                                />
                            </Field>
                        )}

                        {mode === "hours" && (
                            <div className="grid grid-cols-2 gap-3">
                                <Field label={t("CronBuilder_Hours_N")}>
                                    <Input
                                        type="number"
                                        min={1}
                                        max={23}
                                        value={hoursN}
                                        onChange={(event) => setHoursN(event.target.value)}
                                    />
                                </Field>
                                <Field label={t("CronBuilder_Hours_Minute")}>
                                    <Input
                                        type="number"
                                        min={0}
                                        max={59}
                                        value={hoursM}
                                        onChange={(event) => setHoursM(event.target.value)}
                                    />
                                </Field>
                            </div>
                        )}

                        {mode === "daily" && (
                            <Field label={t("CronBuilder_Time")}>
                                <Input
                                    type="time"
                                    value={dailyTime}
                                    onChange={(event) => setDailyTime(event.target.value)}
                                />
                            </Field>
                        )}

                        {mode === "weekly" && (
                            <div className="space-y-3">
                                <Field label={t("CronBuilder_Time")}>
                                    <Input
                                        type="time"
                                        value={weeklyTime}
                                        onChange={(event) => setWeeklyTime(event.target.value)}
                                    />
                                </Field>
                                <fieldset>
                                    <legend className="text-small-label font-medium text-slate-700 dark:text-slate-200">
                                        {t("CronBuilder_Weekly_Days")}
                                    </legend>
                                    <div className="mt-1 flex flex-wrap gap-3">
                                        {WEEKDAYS.map((day) => {
                                            const id = `cron-weekday-${day}`;
                                            return (
                                                <label
                                                    key={day}
                                                    htmlFor={id}
                                                    className="flex items-center gap-1.5 text-body text-slate-700 dark:text-slate-200"
                                                >
                                                    <input
                                                        id={id}
                                                        type="checkbox"
                                                        checked={weeklyDays.includes(day)}
                                                        onChange={(event) =>
                                                            setWeeklyDays((current) =>
                                                                event.target.checked
                                                                    ? [...current, day]
                                                                    : current.filter(
                                                                          (d) => d !== day,
                                                                      ),
                                                            )
                                                        }
                                                        className="h-4 w-4"
                                                    />
                                                    {t(`CronBuilder_Weekday_${day}`)}
                                                </label>
                                            );
                                        })}
                                    </div>
                                </fieldset>
                            </div>
                        )}

                        {mode === "monthly" && (
                            <div className="grid grid-cols-2 gap-3">
                                <Field label={t("CronBuilder_Monthly_Day")}>
                                    <Input
                                        type="number"
                                        min={1}
                                        max={28}
                                        value={monthlyDay}
                                        onChange={(event) => setMonthlyDay(event.target.value)}
                                    />
                                </Field>
                                <Field label={t("CronBuilder_Time")}>
                                    <Input
                                        type="time"
                                        value={monthlyTime}
                                        onChange={(event) => setMonthlyTime(event.target.value)}
                                    />
                                </Field>
                            </div>
                        )}

                        {mode === "custom" && (
                            <div className="grid grid-cols-5 gap-2">
                                <Field label={t("CronBuilder_Custom_Minute")}>
                                    <Input
                                        value={customMinute}
                                        onChange={(event) => setCustomMinute(event.target.value)}
                                    />
                                </Field>
                                <Field label={t("CronBuilder_Custom_Hour")}>
                                    <Input
                                        value={customHour}
                                        onChange={(event) => setCustomHour(event.target.value)}
                                    />
                                </Field>
                                <Field label={t("CronBuilder_Custom_DayOfMonth")}>
                                    <Input
                                        value={customDom}
                                        onChange={(event) => setCustomDom(event.target.value)}
                                    />
                                </Field>
                                <Field label={t("CronBuilder_Custom_Month")}>
                                    <Input
                                        value={customMonth}
                                        onChange={(event) => setCustomMonth(event.target.value)}
                                    />
                                </Field>
                                <Field label={t("CronBuilder_Custom_DayOfWeek")}>
                                    <Input
                                        value={customDow}
                                        onChange={(event) => setCustomDow(event.target.value)}
                                    />
                                </Field>
                            </div>
                        )}

                        <div className="rounded-md border border-slate-200 bg-slate-50 p-3 dark:border-navy-600 dark:bg-navy-900">
                            <p className="text-small-label font-medium text-slate-700 dark:text-slate-200">
                                {t("CronBuilder_Expression")}
                            </p>
                            <code className="mt-1 block font-mono text-body text-slate-900 dark:text-slate-100">
                                {result ? result.expression : "-"}
                            </code>
                            <p className="mt-2 text-body text-slate-600 dark:text-slate-300">
                                {result ? result.preview : t("CronBuilder_Preview_Invalid")}
                            </p>
                        </div>
                    </div>

                    <div className="mt-5 flex justify-end gap-2">
                        <Button
                            type="button"
                            variant="secondary"
                            onClick={() => onOpenChange(false)}
                        >
                            {t("Cancel")}
                        </Button>
                        <Button type="button" onClick={handleApply} disabled={!result}>
                            {t("CronBuilder_Apply")}
                        </Button>
                    </div>
                </Dialog.Content>
            </Dialog.Portal>
        </Dialog.Root>
    );
}
