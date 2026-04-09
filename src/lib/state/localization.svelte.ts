import { loadTranslations } from '$lib/services/localization';
import type { Locale, TranslationKey, Translations } from '$lib/types/localization';
import '$lib/utils/stringExtensions';
import { createLogger } from '$lib/utils/logger';

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
            log.debug('Translation requested', { key: key });
            const parts = key.split('.');
            
            let current: any = translations;
            for (const part of parts) {
                if (current[part] === undefined) return key;
                current = current[part];
            }
            
            const template = typeof current === 'string' ? current : `{${key}}`;
            if (!args) {
                log.debug('Obtained translation', { key, result: template });
                return template;
            }
            log.debug('Obtained translation template', { key, template });

            const format = template.format(args);
            log.debug('Formatted template', { key, template, args, format })
            return format;
        },

        async setLocale(value: Locale) {
            translations = await loadTranslations(value);
            locale = value;
        }
    };
}