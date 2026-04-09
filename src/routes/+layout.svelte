<script lang="ts">
  import '../app.css';
  import Titlebar from '$lib/components/Titlebar.svelte';
  import Sidebar from '$lib/components/Sidebar.svelte';
  import Statusbar from '$lib/components/Statusbar.svelte';
  import { onMount } from 'svelte';
  import { initLocalization } from '$lib/state/localization.svelte';

  onMount(async () => {
    await initLocalization('en');
  });
</script>

<div class="flex flex-col h-dvh overflow-hidden bg-zinc-900 border-zinc-500 border-4">
  <Titlebar/>
  <div class="flex-1 flex flex-row">
    <Sidebar/>
    <main class="flex-1 p-2">
      <slot/>
    </main>
  </div>
  <Statusbar/>
</div>