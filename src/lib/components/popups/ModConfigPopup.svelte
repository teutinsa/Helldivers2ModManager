<script lang="ts">
    import { onMount } from "svelte";
    import { SvelteMap } from "svelte/reactivity";
    import { convertFileSrc } from "@tauri-apps/api/core";
    import { join } from "@tauri-apps/api/path";
    import type { ModConfigPopup } from "$lib/types/popup";
    import type { v1 } from "$lib/models/manifest";
    import { useLocalization } from "$lib/state/localization.svelte";
    import PopupBase from "./PopupBase.svelte";
    import type { ConfigV1 } from "$lib/models/profile";

    const { t } = useLocalization();

    let { popup }: { popup: ModConfigPopup } = $props();
    let toggled = $state<boolean[]>([]);
    let selected = $state<number[]>([]);
    let imagePaths = new SvelteMap<string, string>();

    onMount(async () => {
        const { config, mod } = popup;

        if (config.Guid !== mod.Manifest.Guid) {
            throw new Error("GUIDs of config and mod do not match!");
        }

        if (
            !("Version" in mod.Manifest) ||
            mod.Manifest.Version !== 1 ||
            config.For !== "V1"
        ) {
            throw new Error("Config popup can only handle V1 mods!");
        }

        const manifest = (mod.Manifest as v1.Manifest);

        if (!manifest.Options) {
            throw new Error("Manifest has no options!");
        }

        toggled = [...config.Toggled];
        selected = [...config.Selected];

        for (const opt of manifest.Options) {
            if (opt.Image) {
                imagePaths.set(opt.Image, convertFileSrc(await join(mod.Directory, opt.Image)));
            }
            if (opt.SubOptions) {
                for (const sub of opt.SubOptions) {
                    if (sub.Image) {
                        imagePaths.set(sub.Image, convertFileSrc(await join(mod.Directory, sub.Image)));
                    }
                }
            }
        }
    });

    function onClose() {
        const config = (popup.config as ConfigV1);
        config.Toggled = toggled;
        config.Selected = selected;
        popup.close(config);
    }
</script>

<PopupBase>
    <h1 class="self-center text-4xl">{popup.mod.Manifest.Name}</h1>
    <div class="pr-1 flex flex-col gap-1 overflow-x-hidden overflow-y-auto">
        {#each (popup.mod.Manifest as v1.Manifest).Options! as option, i}
            {@const image = option.Image ? imagePaths.get(option.Image) : undefined}
            <div class="flex flex-row gap-1">
                <img
                    class="object-contain self-start shrink-0"
                    src={image ?? "images/hd2_icon.png"}
                    alt="Option icon"
                    width="64"
                    height="64"
                />
                <div class="flex flex-col flex-1 self-stretch min-w-0">
                    <h2 class="text-xl">{option.Name}</h2>
                    <p class="flex-1 text-sm truncate" title={option.Description}>{option.Description}</p>
                    {#if option.SubOptions}
                        <select
                            bind:value={selected[i]}
                            class="hd2mm-select"
                        >
                            {#each option.SubOptions as subOption, j}
                                {@const subImage = subOption.Image ? imagePaths.get(subOption.Image) : undefined}
                                <option
                                    class="flex flex-row text-lg truncate"
                                    value={j}
                                    title={subOption.Description}
                                >
                                    {subOption.Name}
                                </option>
                            {/each}
                        </select>
                    {/if}
                </div>
                <input
                    bind:checked={toggled[i]}
                    class="w-5 h-5 mt-1 self-start bg-transparent accent-yellow-300 border-zinc-500 border-2"
                    type="checkbox"
                />
            </div>
            {#if i < (popup.mod.Manifest as v1.Manifest).Options!.length - 1}
                <hr class="border-t border-zinc-500" />
            {/if}
        {/each}
    </div>
    <button class="hd2mm-button self-end" onclick={onClose}>{t("popup.config.ok_button.text")}</button>
</PopupBase>