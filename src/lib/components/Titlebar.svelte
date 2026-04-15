<!--
  @component
  A window titlebar providing the standard buttons and drag functionality.
-->

<script lang="ts">
    import { getCurrentWindow } from "@tauri-apps/api/window";
    import { useLocalization } from "$lib/state/localization.svelte";
    import {
        DashLg,
        Fullscreen,
        XLg
    } from "svelte-bootstrap-icons";

    const { t } = useLocalization();
    const appWindow = getCurrentWindow();

    function close() {
        if (import.meta.env.DEV) appWindow.minimize();
        else appWindow.close();
    }
</script>

<div
    class="h-16 flex items-center justify-between pl-1 pb-1 bg-zinc-800 border-zinc-500 border-b-2"
    data-tauri-drag-region
>
    <div class="flex self-stretch">
        <img
            class="w-40"
            src="images/hd2_logo.png"
            alt="HD2 logo"
            data-tauri-drag-region
        />
        <span
            class="text-6xl text-zinc-100 font-blockletter"
            data-tauri-drag-region>{t("app.title")}</span
        >
    </div>

    <div class="flex h-8 self-start">
        <button
            class="w-12 h-8 text-zinc-400 border-zinc-500 border-b-2 border-l-2 hover:border-yellow-300 active:text-zinc-900 active:border-yellow-300 active:bg-yellow-300"
            onclick={() => appWindow.minimize()}
            aria-label="Minimize"
        >
            <DashLg class="block ml-auto mr-auto" width="24" height="24" />
        </button>
        <button
            class="w-12 h-8 text-zinc-400 border-zinc-500 border-b-2 border-l-2 hover:border-yellow-300 active:text-zinc-900 active:border-yellow-300 active:bg-yellow-300"
            onclick={() => appWindow.toggleMaximize()}
            aria-label="Maximize"
        >
            <Fullscreen class="block ml-auto mr-auto" width="24" height="24" />
        </button>
        <button
            class="w-12 h-8 text-zinc-400 border-zinc-500 border-b-2 border-l-2 hover:border-red-600 active:text-zinc-100 active:border-red-600 active:bg-red-600"
            onclick={close}
            aria-label="Close"
        >
            <XLg class="block ml-auto mr-auto" width="24" height="24" />
        </button>
    </div>
</div>
