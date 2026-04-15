import type { Component } from "svelte";
import ConfirmPopupComponent from "$lib/components/popups/ConfirmPopup.svelte";
import InputPopupComponent from "$lib/components/popups/InputPopup.svelte";

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
        public minLength: number = 0,
        public maxLength: number = 0
    ) {
        super();
    }
}