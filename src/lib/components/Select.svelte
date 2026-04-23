<script lang="ts" generics="T">
    import type { Snippet } from 'svelte';
    import type { ClassValue } from 'svelte/elements';

    let {
        items,
        renderItem,
        selectedIndex = $bindable<number>(),
        class: extraClasses
    }: {
        items: T[];
        renderItem?: Snippet<[T]>;
        selectedIndex?: number;
        class?: ClassValue;
    } = $props();
</script>

<select
    bind:value={selectedIndex}
    class="hd2mm-select px-2 py-1 overflow-y-hidden min-w-0 {extraClasses}"
>
    <button>
        <div>
            <selectedcontent></selectedcontent>
        </div>
    </button>
    <div class="border-2 border-zinc-500">
        {#each items as item, i}
            <option
                value={i}
                selected={i === selectedIndex}
            >
                {#if renderItem}
                    {@render renderItem(item)}
                {:else}
                    {String(item)}
                {/if}
            </option>
        {/each}
    </div>
</select>

<style>
    select, select::picker(select) {
        appearance: base-select;
    }
</style>