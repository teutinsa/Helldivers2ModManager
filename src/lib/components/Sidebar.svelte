<!--
  @component
  The side bar for navigation.
-->

<script lang="ts">
    import { page } from "$app/state";
    import {
        Boxes,
        CodeSquare,
        Question,
        Gear,
    } from "svelte-bootstrap-icons";
    import { useLocalization } from "$lib/state/localization.svelte";

    const { t } = useLocalization();
</script>

<div class="w-10 bg-zinc-800 p-1 flex flex-col gap-0.5 justify-between">
    <div class="flex flex-col gap-0.5">
        <a href="/">
            <button
                class="w-8 h-8 p-1 hover:bg-zinc-700 border-2 border-transparent transition
                    {page.url.pathname === '/'
                    ? 'text-yellow-300 border-r-yellow-300'
                    : 'text-zinc-300'}"
                aria-label="Mods"
                title={t("sidebar.mods_button.tip")}
            >
                <Boxes class="w-full h-full block ml-auto mr-auto" />
            </button>
        </a>
        <a href="/create">
            <button
                class="w-8 h-8 p-1 hover:bg-zinc-700 border-2 border-transparent transition
                    {page.url.pathname === '/create'
                    ? 'text-yellow-300 border-r-yellow-300'
                    : 'text-zinc-300'}"
                class:active={page.url.pathname === "/create"}
                aria-label="Create"
                title={t("sidebar.create_button.tip")}
            >
                <CodeSquare class="w-full h-full block ml-auto mr-auto" />
            </button>
        </a>
    </div>

    <div class="flex flex-col gap-0.5">
        <a href="/help">
            <button
                class="w-8 h-8 p-1 hover:bg-zinc-700 border-2 border-transparent transition
                    {page.url.pathname === '/help'
                    ? 'text-yellow-300 border-r-yellow-300'
                    : 'text-zinc-300'}"
                class:active={page.url.pathname === "/help"}
                aria-label="Help"
                title={t("sidebar.help_button.tip")}
            >
                <Question class="w-full h-full block ml-auto mr-auto" />
            </button>
        </a>
        <a href="/settings">
            <button
                class="w-8 h-8 p-1 hover:bg-zinc-700 border-2 border-transparent transition
                    {page.url.pathname === '/settings'
                    ? 'text-yellow-300 border-r-yellow-300'
                    : 'text-zinc-300'}"
                class:active={page.url.pathname === "/settings"}
                aria-label="Settings"
                title={t("sidebar.settings_button.tip")}
            >
                <Gear class="w-full h-full block ml-auto mr-auto" />
            </button>
        </a>
    </div>
</div>
