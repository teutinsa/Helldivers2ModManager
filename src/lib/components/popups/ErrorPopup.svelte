<script lang="ts">
    import PopupBase from "./PopupBase.svelte";
    import { ErrorPopup } from "$lib/types/popup";
    import { useLocalization } from "$lib/state/localization.svelte";
    
    const { t } = useLocalization();

    let { popup }: { popup: ErrorPopup } = $props();
</script>

<PopupBase>
    <span class="text-red-500 text-xl font-blockletter self-center">{t("popup.notification.title.error")}</span>
    <p class="min-w-40 text-sm self-start whitespace-pre-wrap">{popup.message}</p>
    <pre class="min-w-40 px-2 py-1 bg-zinc-800 rounded text-sm self-start whitespace-pre-wrap font-mono">{popup.errorMessage}</pre>
    <button class="hd2mm-button self-end" onclick={() => popup.close()}>{t("popup.notification.ok_button.text")}</button>
</PopupBase>