<script lang="ts">
    import PopupBase from "./PopupBase.svelte";
    import { NotificationPopup } from "$lib/types/popup";
    import { useLocalization } from "$lib/state/localization.svelte";
    
    const { t } = useLocalization();

    let { popup }: { popup: NotificationPopup } = $props();
</script>

<PopupBase>
    {#if popup.kind == "info"}
        <span class="text-blue-500 text-xl font-blockletter self-center">{t("popup.notification.title.info")}</span>
    {:else if popup.kind == "warning"}
        <span class="text-yellow-300 text-xl font-blockletter self-center">{t("popup.notification.title.warning")}</span>
    {:else if popup.kind == "error"}
        <span class="text-red-500 text-xl font-blockletter self-center">{t("popup.notification.title.error")}</span>
    {/if}
    <p class="min-w-40 text-sm self-start whitespace-pre-wrap">{popup.message}</p>
    <button class="hd2mm-button self-end" onclick={() => popup.close()}>{t("popup.notification.ok_button.text")}</button>
</PopupBase>