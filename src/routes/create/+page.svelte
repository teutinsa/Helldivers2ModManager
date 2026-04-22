<script lang="ts">
    import UnderConstruction from "$lib/components/UnderConstruction.svelte";
    import { useLocalization } from "$lib/state/localization.svelte";

    const { t } = useLocalization();
</script>

<div class="w-full h-full flex flex-col gap-1 items-center justify-center">
    <UnderConstruction />
</div>