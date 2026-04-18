import type { Popup } from '$lib/types/popup';

let popups = $state<Popup<any>[]>([]);

export function usePopup() {
    return {
        get isShown(): boolean {
            return popups.length > 0;
        },

        get currentPopup(): Popup<any> {
            return popups[popups.length - 1];
        },

        async show<T = void>(popup: Popup<T>): Promise<T> {
            popups.push(popup);
            try {
                return await popup.promise;
            } finally {
                popups = popups.filter((p) => p !== popup);
            }
        }
    }
}