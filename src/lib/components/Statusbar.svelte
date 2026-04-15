<!--
  @component
  A statusbar.
-->

<script lang="ts">
  import { useLocalization } from '$lib/state/localization.svelte';
    import { Discord, Globe, HeartFill } from 'svelte-bootstrap-icons';
  
  const { t } = useLocalization();
</script>

<div class="h-8 flex items-center p-1 bg-zinc-800 gap-2">
  <span class="text-zinc-100 bg-zinc-900 rounded-xs px-1.5">{t("app.version", [__APP_VERSION__])}</span>
  <span class="flex-1"></span>
  <a class="flex flex-row gap-0.5 text-zinc-100 bg-zinc-900 rounded-xs px-1.5 items-center" href="https://ko-fi.com/teutinsa">
    <HeartFill class="text-pink-300" />
    <span class="underline">Support me!</span>
  </a>
  <a class="flex flex-row gap-0.5 text-zinc-100 bg-zinc-900 rounded-xs px-1.5 items-center" href="https://discord.gg/ZwjPaZNwH7">
    <Discord class="text-indigo-400" />
    <span class="underline">Discord</span>
  </a>
  <a class="flex flex-row gap-0.5 text-zinc-100 bg-zinc-900 rounded-xs px-1.5 items-center" href="https://teutinsa.github.io/">
    <Globe class="text-yellow-300" />
    <span class="underline">hd2mm.io</span>
  </a>
</div>