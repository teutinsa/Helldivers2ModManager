<script lang="ts">
    import PopupBase from "./PopupBase.svelte";
    import { WaitPopup } from "$lib/types/popup";
    
    let { popup }: { popup: WaitPopup } = $props();
</script>

<PopupBase>
    <div
        class="w-6 h-6 rounded-full border-4 border-transparent border-b-yellow-300 animate-spin self-center"
    ></div>
    <span class="self-center">{popup.message}</span>
</PopupBase>