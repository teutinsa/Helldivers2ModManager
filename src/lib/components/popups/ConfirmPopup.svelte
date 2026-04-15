<script lang="ts">
    import PopupBase from "./PopupBase.svelte";
    import { ConfirmPopup } from "$lib/types/popup";
    import { useLocalization } from "$lib/state/localization.svelte";
    const { t } = useLocalization();

    let { popup }: { popup: ConfirmPopup } = $props();
</script>

<PopupBase>
    <span class="text-xl self-center">{popup.title}</span>
    <p class="text-sm">{popup.question}</p>
    <div class="flex flex-row gap-2 justify-between">
        <button class="hd2mm-button" onclick={() => popup.close(true)}>{t("popup.confirm.yes_button.text")}</button>
        <button class="hd2mm-button" onclick={() => popup.close(false)}>{t("popup.confirm.no_button.text")}</button>
    </div>
</PopupBase>