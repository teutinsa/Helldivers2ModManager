<!--
  @component
  A statusbar.
-->

<script lang="ts">
  import { useLocalization } from '$lib/state/localization.svelte';
  
  const { t } = useLocalization();
</script>

<div class="h-8 flex items-center justify-between p-1 bg-zinc-800 select-none">
  <span class="text-zinc-100 bg-zinc-900 rounded-xs px-1.5">{t("app.version", [__APP_VERSION__])}</span>
</div>