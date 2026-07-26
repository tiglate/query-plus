import i18n from "i18next";
import LanguageDetector from "i18next-browser-languagedetector";
import { initReactI18next } from "react-i18next";
import en from "./en.json";
import ptBR from "./pt-BR.json";

export type SupportedLocale = "pt-BR" | "en";

export function normalizeLocale(value: string | null | undefined): SupportedLocale | null {
    if (!value) return null;
    const decoded = decodeURIComponent(value).toLowerCase();
    if (decoded.startsWith("pt")) return "pt-BR";
    if (decoded.startsWith("en")) return "en";
    return null;
}

export function detectCookieLocale(cookie = document.cookie): SupportedLocale | null {
    const names = ["QueryPlus.Culture", ".AspNetCore.Culture", "AspNetCore.Culture"];
    for (const name of names) {
        const entry = cookie
            .split(";")
            .map((part) => part.trim())
            .find((part) => part.startsWith(`${name}=`));
        if (!entry) continue;
        const raw = entry.slice(name.length + 1);
        const uiCulture = /(?:^|[|&])uic=([^|&]+)/i.exec(decodeURIComponent(raw))?.[1];
        const culture = /(?:^|[|&])c=([^|&]+)/i.exec(decodeURIComponent(raw))?.[1];
        const locale = normalizeLocale(uiCulture ?? culture ?? raw);
        if (locale) return locale;
    }
    return null;
}

function initialLocale(): SupportedLocale | undefined {
    const cookieLocale = detectCookieLocale();
    if (cookieLocale) return cookieLocale;
    try {
        return normalizeLocale(localStorage.getItem("i18nextLng")) ?? undefined;
    } catch {
        return undefined;
    }
}

void i18n
    .use(LanguageDetector)
    .use(initReactI18next)
    .init({
        resources: { en: { translation: en }, "pt-BR": { translation: ptBR } },
        lng: initialLocale(),
        fallbackLng: "pt-BR",
        supportedLngs: ["pt-BR", "en"],
        nonExplicitSupportedLngs: false,
        interpolation: { escapeValue: false },
        showSupportNotice: false,
        detection: { order: ["localStorage", "navigator"], caches: ["localStorage"] },
        react: { useSuspense: false },
    });

export async function setLocale(locale: SupportedLocale): Promise<void> {
    await i18n.changeLanguage(locale);
    document.documentElement.lang = locale;
}

export default i18n;
