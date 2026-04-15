import { loadTranslations } from '$lib/services/localization';
import type { Locale, TranslationKey, Translations } from '$lib/types/localization';
import { createLogger } from '$lib/utils/logger';
import '$lib/utils/stringExtensions';

const log = createLogger('LocalizationService')
let locale = $state<Locale>('en');
let translations = $state<Translations>({});

export async function initLocalization(defaultLocale: Locale = 'en') {
    translations = await loadTranslations(defaultLocale);
    locale = defaultLocale;
}

export function useLocalization() {
    return {
        get locale() {
            return locale;
        },
        
        t(key: TranslationKey, args?: Record<string, unknown> | unknown[]): string {
            const parts = key.split('.');
            
            let current: any = translations;
            for (const part of parts) {
                if (current[part] === undefined) return key;
                current = current[part];
            }
            
            if (typeof current !== 'string') {
                log.error("Key not found!", { key })
                return `{${key}}`;
            }

            const template = current as string;
            if (!args)
                return template;

            const format = template.format(args);
            return format;
        },

        async setLocale(value: Locale) {
            translations = await loadTranslations(value);
            locale = value;
        }
    };
}