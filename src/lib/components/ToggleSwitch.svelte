<script lang="ts">
    import type { ChangeEventHandler } from "svelte/elements";

    let {
        checked = $bindable(false),
        onchange,
    }: {
        checked: boolean;
        onchange?: ChangeEventHandler<HTMLInputElement>
    } = $props();
</script>

<label class="relative inline-flex items-center cursor-pointer">
    <input type="checkbox" class="sr-only peer" bind:checked {onchange} />
    <!-- Track -->
    <div
        class="w-11 h-6 bg-zinc-700 rounded-full peer-checked:bg-yellow-300 transition-colors duration-300"
    ></div>
    <!-- Thumb -->
    <div
        class="absolute left-1 top-1 w-4 h-4 bg-zinc-300 rounded-full transition-transform duration-300 peer-checked:translate-x-5"
    ></div>
</label>
