<!--
  @component
  The side bar for navigation.
-->

<script lang="ts">
    import { page } from '$app/state';
</script>

<div class="w-10 bg-zinc-800 p-1 flex flex-col justify-between">
    <div class="flex flex-col">
        <a href="/">
            <button class="w-8 h-8 rounded-md hover:bg-zinc-700 border-2 border-transparent {page.url.pathname === '/' ? 'border-yellow-300' : ''}" aria-label="Mods" title="Mods">
                <img class="block ml-auto mr-auto" src="/icons/box.svg" alt="Mods"/>
            </button>
        </a>
        <a href="/create">
            <button class="w-8 h-8 rounded-md hover:bg-zinc-700 border-2 border-transparent {page.url.pathname === '/create' ? 'border-yellow-300' : ''}" class:active={page.url.pathname === '/create'} aria-label="Create" title="Create">
                <img class="block ml-auto mr-auto" src="/icons/toolbox.svg" alt="Create"/>
            </button>
        </a>
    </div>
    <div class="flex flex-col gap-1">
        <a href="/help">
            <button class="w-8 h-8 text-zinc-100 rounded-md hover:bg-zinc-700 border-2 border-transparent {page.url.pathname === '/help' ? 'border-yellow-300' : ''}" class:active={page.url.pathname === '/help'} aria-label="Help" title="Help">?</button>
        </a>
        <a href="/settings">
            <button class="w-8 h-8 rounded-md hover:bg-zinc-700 border-2 border-transparent {page.url.pathname === '/settings' ? 'border-yellow-300' : ''}" class:active={page.url.pathname === '/settings'} aria-label="Settings" title="Settings">
                <img class="block ml-auto mr-auto" src="/icons/settings.svg" alt="Settings"/>
            </button>
        </a>
    </div>
</div>