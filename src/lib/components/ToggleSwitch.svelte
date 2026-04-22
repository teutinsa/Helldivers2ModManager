<script lang="ts">
    let {
        checked = $bindable(false),
        onchange
    }: {
        checked: boolean;
        onchange?: () => void;
    } = $props();
</script>

<button
    class="relative inline-flex items-center cursor-pointer"
    onpointerdown={(e) => e.stopPropagation()}
    onclick={() => {
        checked = !checked;
        onchange?.();
    }}
>
    <!-- Track -->
    <div class="w-11 h-6 rounded-full transition-colors duration-300
        {checked ? 'bg-yellow-300' : 'bg-zinc-700'}">
    </div>
    <!-- Thumb -->
    <div class="absolute left-1 top-1 w-4 h-4 bg-zinc-300 rounded-full transition-transform duration-300
        {checked ? 'translate-x-5' : ''}">
    </div>
</button>