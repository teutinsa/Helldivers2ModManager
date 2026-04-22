import type { Component } from "svelte";
import ConfirmPopupComponent from "$lib/components/popups/ConfirmPopup.svelte";
import InputPopupComponent from "$lib/components/popups/InputPopup.svelte";
import WaitPopupComponent from "$lib/components/popups/WaitPopup.svelte";
import NotificationPopupComponent from "$lib/components/popups/NotificationPopup.svelte";
import ErrorPopupComponent from "$lib/components/popups/ErrorPopup.svelte";
import AddResultPopupComponent from "$lib/components/popups/AddResultPopup.svelte";
import type { ModAddResult } from "./results";

export abstract class Popup<T = void> {
    abstract component: Component<any, any, any>;

    private _resolve!: (value: T) => void;
    readonly promise: Promise<T> = new Promise(resolve => {
        this._resolve = resolve;
    });

    close(result: T) {
        console.debug(this);
        this._resolve(result);
    }
}

export class ConfirmPopup extends Popup<boolean> {
    component = ConfirmPopupComponent;

    constructor(
        public readonly title: string,
        public readonly question: string
    ) {
        super();
    }
}

export class InputPopup extends Popup<string | null> {
    component = InputPopupComponent;

    constructor(
        public readonly placeholder: string,
        public readonly allowEmpty: boolean = false,
        public readonly minLength?: number,
        public readonly maxLength?: number,
        public readonly format?: RegExp
    ) {
        super();
    }
}

export class WaitPopup extends Popup {
    component = WaitPopupComponent;

    constructor(public readonly message: string) {
        super();
    }
}

export class NotificationPopup extends Popup {
    component = NotificationPopupComponent;

    constructor(
        public readonly kind: 'info' | 'warning' | 'error',
        public readonly message: string
    ) {
        super();
    }
}

export class ErrorPopup extends Popup {
    component = ErrorPopupComponent;

    constructor(
        public readonly message: string,
        public readonly errorMessage: string
    ) {
        super();
    }
}

export class AddResultPopup extends Popup {
    component = AddResultPopupComponent;

    constructor(public readonly results: ModAddResult[]) {
        super();
    }
}