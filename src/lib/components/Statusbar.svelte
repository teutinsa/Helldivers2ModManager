<!--
  @component
  A statusbar.
-->

<script lang="ts">
    import { useLocalization } from "$lib/state/localization.svelte";
    import { openUrl } from "@tauri-apps/plugin-opener";
    import { Discord, Globe, HeartFill } from "svelte-bootstrap-icons";

    const { t } = useLocalization();
    
    function clickHandler(e: MouseEvent) {
        const target = e.target as Element | null;
        if (!target) return;

        const anchor = target.closest("a");
        if (!anchor) return;

        const url = anchor.getAttribute("href");
        if (!url) return;

        e.preventDefault();
        openUrl(url);
    }
</script>

<div class="h-8 flex items-center p-1 bg-zinc-800 gap-2">
    <span
        class="text-zinc-100 bg-zinc-900 rounded-xs px-1.5"
    >
        {t("app.version", [__APP_VERSION__])}
    </span>
    <span class="flex-1"></span>
    <a
        class="flex flex-row gap-1 text-zinc-100 bg-zinc-900 rounded-xs px-1.5 items-center hover:text-blue-300"
        href="https://ko-fi.com/teutinsa"
        title="https://ko-fi.com/teutinsa"
        onclick={clickHandler}
    >
        <HeartFill class="text-pink-300" />
        <span class="underline">Support me!</span>
    </a>
    <a
        class="flex flex-row gap-1 text-zinc-100 bg-zinc-900 rounded-xs px-1.5 items-center hover:text-blue-300"
        href="https://discord.gg/ZwjPaZNwH7"
        title="https://discord.gg/ZwjPaZNwH7"
        onclick={clickHandler}
    >
        <Discord class="text-indigo-400" />
        <span class="underline">Discord</span>
    </a>
    <a
        class="flex flex-row gap-1 text-zinc-100 bg-zinc-900 rounded-xs px-1.5 items-center hover:text-blue-300"
        href="https://teutinsa.github.io/hd2mm-site"
        title="https://teutinsa.github.io/hd2mm-site"
        onclick={clickHandler}
    >
        <Globe class="text-yellow-300" />
        <span class="underline">hd2mm.io</span>
    </a>
</div>
