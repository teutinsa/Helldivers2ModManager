<script lang="ts">
    import * as fs from "@tauri-apps/plugin-fs";
    import { open } from "@tauri-apps/plugin-dialog";
    import { beforeNavigate, onNavigate } from "$app/navigation";
    import { toSkipEntry, type SkipEntry } from "$lib/models/settings";
    import { useLocalization } from "$lib/state/localization.svelte";
    import { loadSettings, saveSettings } from "$lib/utils/commands";
    import { Dash, Plus, ThreeDots } from "svelte-bootstrap-icons";
    import { usePopup } from "$lib/state/popup.svelte";
    import { InputPopup, NotificationPopup } from "$lib/types/popup";
    import { path } from "@tauri-apps/api";

    const { t } = useLocalization();
    const { show: showPopup } = usePopup();
    
    let gamePath = $state<string>("");
    let skipList = $state<SkipEntry[]>([]);
    let selectedSkipIndex = $state<number>(-1);
    let gamePathErrors = $state<string[]>([]);
    let initPromise = $state<Promise<void>>(init());

    $effect(() => {
        const current = {
            gamePath,
            skipList
        };
        let cancelled = false;

        const validationPromise = async () => {
            const errors = [];
            
            if (!current.gamePath || current.gamePath.length === 0) {
                errors.push(t("pages.settings.validation_error.game_path.empty"));
            } else {
                try {
                    if (!await fs.exists(current.gamePath)) {
                        errors.push(t("pages.settings.validation_error.game_path.exists"));
                    } else {
                        if (!await fs.exists(await path.join(current.gamePath, "tools"))) {
                            errors.push(t("pages.settings.validation_error.game_path.tools_exists"));
                        }
                        
                        if (!await fs.exists(await path.join(current.gamePath, "data"))) {
                            errors.push(t("pages.settings.validation_error.game_path.data_exists"));
                        }
                        
                        if (!await fs.exists(await path.join(current.gamePath, "bin"))) {
                            errors.push(t("pages.settings.validation_error.game_path.bin_exists"));
                        } else if (!await fs.exists(await path.join(current.gamePath, "bin", "helldivers2.exe"))) {
                            errors.push(t("pages.settings.validation_error.game_path.exe_exists"));
                        }
                    }
                } catch {
                    errors.push(t("pages.settings.validation_error.game_path.invalid"));
                }
            }

            if (!cancelled) gamePathErrors = errors;
        };

        validationPromise();
        return () => { cancelled = true; }
    });

    beforeNavigate(({ cancel }) => {
        if (gamePathErrors.length === 0) return;
        cancel();
        showPopup(new NotificationPopup(
            "error",
            t("pages.settings.popup.notification.validation_error.message")
        ));
    });

    onNavigate(async () => {
        await saveSettings({
            Version: "V1",
            GamePath: gamePath,
            SkipList: skipList
        });
    })

    async function init() {
        const settings = await loadSettings();
        switch (settings.Version) {
            case "V1":
                gamePath = settings.GamePath;
                skipList = settings.SkipList;
                break;
        }
    }

    async function onBrowse() {
        const path = await open({
            directory: true,
            multiple: false,
        });
        if (!path) return;
        gamePath = path;
    }

    async function onAddSkipEntry() {
        const input = await showPopup(new InputPopup(
            t("pages.settings.popup.input.add_skip.placeholder"),
            false,
            16,
            16,
            /^[0-9a-f]+$/
        ));
        if (!input) return;
        skipList.push(toSkipEntry(input));
    }

    function onRemoveSkipEntry() {
        if (selectedSkipIndex < 0 || selectedSkipIndex >= skipList.length) return;
        skipList.splice(selectedSkipIndex, 1);
        selectedSkipIndex = -1;
    }
</script>

{#await initPromise}
    <div class="w-full h-full flex flex-col justify-center items-center">
        <div
            class="p-4 bg-zinc-800 border-2 border-zinc-500 flex flex-col gap-1 items-center"
        >
            <div
                class="w-6 h-6 rounded-full border-4 border-transparent border-b-yellow-300 animate-spin"
            ></div>
            <span class="text-zinc-300">{t("pages.settings.loading.text")}</span>
        </div>
    </div>
{:then _}
    <div class="w-full h-full flex flex-col justify-stretch">
        <h1 class="text-zinc-300 text-2xl font-blockletter self-center">{t("pages.settings.title")}</h1>
        <div class="pr-1 flex-1 flex flex-col gap-2 overflow-y-scroll">
            <div class="flex flex-col gap-1">
                <h2 class="text-zinc-300 text-xl">{t("pages.settings.game_path.title")}</h2>
                <div class="flex flex-row gap-1">
                    <input
                        bind:value={gamePath}
                        id="gamepath"
                        class="hd2mm-input flex-1"
                        placeholder={t("pages.settings.game_path.placeholder")}
                        autocomplete="off"
                        autocorrect="off"
                        autocapitalize="off"
                        spellcheck="false"
                    />
                    <button
                        class="hd2mm-button"
                        title={t("pages.settings.game_path.browse_button.tip")}
                        onclick={onBrowse}
                    >
                        <ThreeDots class="m-auto block" />
                    </button>
                </div>
                <ul class="ml-6 text-red-500 list-disc">
                    {#each gamePathErrors as error}
                        <li>{error}</li>
                    {/each}
                </ul>
            </div>
            <div class="flex flex-col gap-1 self-start">
                <h2 class="text-zinc-300 text-xl">{t("pages.settings.skip_list.title")}</h2>
                <ol class="w-80 h-40 border-2 border-zinc-500 overflow-y-scroll">
                    {#each skipList as entry, i (entry)}
                        <li>
                            <button
                                class="w-full px-1 text-zinc-300 font-mono text-start"
                                class:bg-yellow-300={selectedSkipIndex === i}
                                class:text-zinc-900={selectedSkipIndex === i}
                                onclick={() => selectedSkipIndex = i}
                            >
                                {entry}
                            </button>
                        </li>
                    {/each}
                </ol>
                <div class="flex flex-row gap-1 justify-end">
                    <button
                        class="hd2mm-button"
                        onclick={onAddSkipEntry}
                    >
                        <Plus class="m-auto block" />
                    </button>
                    <button
                        class="hd2mm-button"
                        disabled={selectedSkipIndex < 0 || selectedSkipIndex >= skipList.length}
                        onclick={onRemoveSkipEntry}
                    >
                        <Dash class="m-auto block" />
                    </button>
                </div>
            </div>
        </div>
    </div>
{:catch ex}
    <div class="w-full h-full flex justify-center items-center">
        <div class="p-4 bg-zinc-800 border-2 border-zinc-500 flex flex-col">
            <span class="text-red-500 text-xl self-center">
                {t("pages.settings.loading_failed.title")}
            </span>
            <p class="text-zinc-300 text-sm font-mono">{ex.toString()}</p>
        </div>
    </div>
{/await}