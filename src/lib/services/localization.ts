import type { Locale, Translations } from '$lib/types/localization';

export async function loadTranslations(locale: Locale): Promise<Translations> {
    const module = await import(`$lib/locales/${locale}.json`);
    return module.default;
}