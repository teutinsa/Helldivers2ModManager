<script lang="ts">
    import PopupBase from "./PopupBase.svelte";
    import { InputPopup } from "$lib/types/popup";
    import { useLocalization } from "$lib/state/localization.svelte";

    const { t } = useLocalization();

    let { popup }: { popup: InputPopup } = $props();
    let input = $state<string>("");
    let errors = $state<string[]>([]);

    let isValid = $derived<boolean>(errors.length == 0);

    $effect(() => {
        const newErrors = [];

        if (!popup.allowEmpty && input.length == 0) {
            newErrors.push(t("popup.input.errors.empty"));
        }

        if (popup.minLength && popup.maxLength) {
            if (input.length < popup.minLength || input.length > popup.maxLength) {
                if (popup.minLength === popup.maxLength) {
                    newErrors.push(t("popup.input.errors.exact_length", [popup.maxLength]));
                } else {
                    newErrors.push(
                        t("popup.input.errors.range_length", [
                            popup.minLength,
                            popup.maxLength,
                        ]),
                    );
                }
            }
        } else {
            if (popup.minLength && input.length < popup.minLength) {
                newErrors.push(
                    t("popup.input.errors.min_length", [popup.minLength]),
                );
            }
            if (popup.maxLength && input.length < popup.maxLength) {
                newErrors.push(
                    t("popup.input.errors.max_length", [popup.maxLength]),
                );
            }
        }

        if (popup.format && !popup.format.test(input)) {
            newErrors.push(t("popup.input.errors.format", [popup.format]));
        }

        errors = newErrors;
    });
</script>

<PopupBase>
    <input
        class="hd2mm-input w-80"
        id="input"
        type="text"
        placeholder={popup.placeholder}
        autocomplete="off"
        autocorrect="off"
        autocapitalize="off"
        spellcheck="false"
        maxlength={popup.minLength}
        bind:value={input}
    />
    {#if !isValid}
        <ul class="text-red-500 text-sm">
            {#each errors as error}
                <li>{error}</li>
            {/each}
        </ul>
    {/if}
    <div class="flex flex-row gap-2 justify-between">
        <button
            class="hd2mm-button"
            onclick={() => popup.close(input)}
            disabled={!isValid}>{t("popup.input.confirm_button.text")}</button
        >
        <button class="hd2mm-button" onclick={() => popup.close(null)}
            >{t("popup.input.abort_button.text")}</button
        >
    </div>
</PopupBase>
