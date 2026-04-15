<script lang="ts">
    import { usePopup } from "$lib/state/popup.svelte";

    const popup = usePopup();
</script>

{#if popup.isShown}
    <div class="absolute inset-0 z-50 bg-black/20 flex justify-center items-center">
        <popup.currentPopup.component popup={popup.currentPopup} />
    </div>
{/if}