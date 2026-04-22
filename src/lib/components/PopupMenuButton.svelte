<script lang="ts">
    import type { Snippet } from "svelte";
    import { ThreeDotsVertical } from "svelte-bootstrap-icons";

    let isOpen = $state<boolean>(false);
    let x = $state<number>(0);
    let y = $state<number>(0);
    let button = $state<HTMLElement | null>(null);
    let popup = $state<HTMLElement | null>(null);

    let { children, insertTarget }: { children: Snippet, insertTarget?: string | HTMLElement } = $props();

    $effect(() => {
        if (!popup) return;

        const popupRect = popup.getBoundingClientRect();
        const maxX = window.innerWidth - popupRect.width - 8;
        x = Math.min(x, maxX);
        const maxY = window.innerHeight - popupRect.height - 8;
        y = Math.min(y, maxY);
    });

    function clickOutside(node: HTMLElement, onClose: () => void) {
        function handler(e: MouseEvent) {
            if (!node.contains(e.target as Node)) onClose();
        }
        document.addEventListener("mousedown", handler);
        return { destroy() { document.removeEventListener("mousedown", handler); } };
    }

    function windowResize(_: HTMLElement, onClose: () => void) {
        function handler(_: UIEvent) {
            onClose();
        }
        window.addEventListener("resize", handler);
        return { destroy() { window.removeEventListener("resize", handler); } }
    }

    function portal(node: HTMLElement, target: string | HTMLElement) {
        function update(target: string | HTMLElement) {
            const elm = typeof target == "string"
                ? document.querySelector(target) as HTMLElement
                : target;
            elm.appendChild(node);
        }
        update(target);
        return { update, destroy() { node.remove(); } }
    }
    
    function onClick() {
        isOpen = true;

        const rect = button!.getBoundingClientRect();
        x = rect.left;
        y = rect.bottom + 4;
    }
</script>

<button
    bind:this={button}
    class="hd2mm-button-nop p-2"
    class:border-yellow-300={isOpen}
    onclick={onClick}
>
    <ThreeDotsVertical class="block mx-auto" />
</button>
{#if isOpen}
    <div
        bind:this={popup}
        use:clickOutside={() => isOpen = false}
        use:windowResize={() => isOpen = false}
        use:portal={insertTarget ?? "body"}
        class="fixed border-2 border-zinc-500 bg-zinc-800 drop-shadow-xl/50 transform-none m-0 z-40"
        style:top="{y}px"
        style:left="{x}px"
        onclickcapture={() => isOpen = false}
    >
        <div class="flex flex-col items-stretch popup-content">
            {@render children()}
        </div>
    </div>
{/if}