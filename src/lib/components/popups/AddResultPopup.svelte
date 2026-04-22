<script lang="ts">
    import * as path from "@tauri-apps/api/path";
    import PopupBase from "./PopupBase.svelte";
    import { AddResultPopup } from "$lib/types/popup";
    import { useLocalization } from "$lib/state/localization.svelte";
    import { CheckSquare, XSquare } from "svelte-bootstrap-icons";
    
    const { t } = useLocalization();

    let { popup }: { popup: AddResultPopup } = $props();
</script>

<PopupBase>
    <span class="text-yellow-300 text-xl font-blockletter self-center">{t("popup.add_result.title")}</span>
    <ul class="min-w-60 flex flex-col items-stretch gap-1 overflow-y-auto overflow-x-hidden">
        {#each popup.results as result}
            <li class="flex flex-col gap-0.5">
                <div class="flex flex-row items-center gap-2">
                    {#if result.success}
                        <CheckSquare class="text-green-500" />
                    {:else}
                        <XSquare class="text-red-500" />
                    {/if}
                    {#await path.basename(result.archiveFile) then archiveFile}
                        <pre class="font-mono truncate">{archiveFile}</pre>
                    {/await}
                </div>
                {#if !result.success}
                    <pre class="font-mono ml-6 bg-zinc-700 rounded px-1 whitespace-pre-wrap break-normal">{result.errorMessage}</pre>
                {/if}
            </li>
        {/each}
    </ul>
    <button class="hd2mm-button self-stretch" onclick={() => popup.close()}>{t("popup.add_result.ok_button.text")}</button>
</PopupBase>