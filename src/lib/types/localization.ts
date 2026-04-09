export type Locale = 'en' | 'de';
export type TranslationKey = string;

export interface Translations {
    [key: TranslationKey]: string;
}