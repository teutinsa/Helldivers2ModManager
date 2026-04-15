<script lang="ts">
    import PopupBase from "./PopupBase.svelte";
    import { InputPopup } from "$lib/types/popup";
    import { useLocalization } from "$lib/state/localization.svelte";
    
    const { t } = useLocalization();

    let { popup }: { popup: InputPopup } = $props();
    let input = $state<string>("");
</script>

<PopupBase>
    <input class="hd2mm-input" type="text" placeholder={popup.placeholder} bind:value={input} />
    <div class="flex flex-row gap-2 justify-between">
        <button class="hd2mm-button" onclick={() => popup.close(input)}>{t("popup.input.confirm_button.text")}</button>
        <button class="hd2mm-button" onclick={() => popup.close(null)}>{t("popup.input.abort_button.text")}</button>
    </div>
</PopupBase>