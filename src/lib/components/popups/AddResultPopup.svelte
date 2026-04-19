<script lang="ts">
    import PopupBase from "./PopupBase.svelte";
    import { AddResultPopup } from "$lib/types/popup";
    import { useLocalization } from "$lib/state/localization.svelte";
    
    const { t } = useLocalization();

    let { popup }: { popup: AddResultPopup } = $props();
</script>

<PopupBase>
    <span class="text-red-500 text-xl font-blockletter self-center">{t("popup.add_result.title")}</span>
    <pre class="min-w-40 px-2 py-1 bg-zinc-800 rounded text-sm self-start whitespace-pre-wrap font-mono">
        
    </pre>
    <button class="hd2mm-button self-end" onclick={() => popup.close()}>{t("popup.add_result.ok_button.text")}</button>
</PopupBase>