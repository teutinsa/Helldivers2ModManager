<script lang="ts">
    import "../app.css";
    import Titlebar from "$lib/components/Titlebar.svelte";
    import Sidebar from "$lib/components/Sidebar.svelte";
    import Statusbar from "$lib/components/Statusbar.svelte";
    import Popup from "$lib/components/Popup.svelte";
    import { onMount } from "svelte";
    import { initLocalization } from "$lib/state/localization.svelte";
    import { checkSettings } from "$lib/utils/commands";
    import { goto } from "$app/navigation";

    onMount(async () => {
        await initLocalization("en");

        if (!await checkSettings()) {
            goto("/settings");
        }
    });
</script>

<div
    class="flex flex-col h-dvh overflow-hidden bg-zinc-900 border-zinc-500 border-4"
>
    <Titlebar />
    <div class="relative flex-1 flex flex-row">
        <Sidebar />
        <main class="flex-1 p-2 overflow-hidden min-w-0">
            <slot />
        </main>
        <Popup />
    </div>
    <Statusbar />
</div>
