<script lang="ts">
    import { onDestroy, onMount } from "svelte";
    import { SvelteMap } from "svelte/reactivity";
    import { Plus, Dash, Backspace, ArrowBarRight, ArrowBarLeft, Arrow90degLeft, ArrowReturnLeft, PencilSquare, Download } from "svelte-bootstrap-icons";
    import { getCurrentWebview } from "@tauri-apps/api/webview";
    import { openUrl } from "@tauri-apps/plugin-opener";
    import { open } from "@tauri-apps/plugin-dialog";
    import { useLocalization } from "$lib/state/localization.svelte";
    import { createLogger } from "$lib/utils/logger";
    import type { Mod } from "$lib/models/mod";
    import type { Config, Profile, ProfileV1 } from "$lib/models/profile";
    import { addMod, getMods, loadProfiles, saveProfiles } from "$lib/utils/commands";
    import type { UUID } from "$lib/types/uuid";
    import { usePopup } from "$lib/state/popup.svelte";
    import {
        ConfirmPopup,
        InputPopup,
        WaitPopup,
        NotificationPopup,
        ErrorPopup
    } from "$lib/types/popup";
    import ToggleSwitch from "$lib/components/ToggleSwitch.svelte";

    const log = createLogger("ModsPage");
    const { t } = useLocalization();
    const { show: showPopup } = usePopup();

    let mods = $state<Mod[]>([]);
    let profiles = $state<Profile[]>([]);
    let activeProfile = $state<number>(0);
    let searchText = $state<string>("");
    let profileConfigs = $state<[UUID, Config][]>([]);
    let iconPaths = new SvelteMap<UUID, string | null>();
    let dragIndex = $state<number | null>(null);
    let libraryExtended = $state<boolean>(false);
    let libraryVisible = $state<boolean>(false);
    let isDragging = $state<boolean>(false);
    let initPromise = $state<Promise<void>>();

    let currentProfile = $derived<Profile | undefined>(profiles[activeProfile]);
    let profileMods = $derived<Mod[]>(profileConfigs.map(([guid, _]) => mods.find(m => m.guid === guid)).filter((m): m is Mod => m !== undefined));
    let profileEntries = $derived<[UUID, Config, Mod][]>(
        profileConfigs
            .map(([guid, config], i) => [guid, config, profileMods[i]] as [UUID, Config, Mod])
            .filter(([_, __, mod]) =>
                searchText.length === 0 ||
                [mod.name, mod.description].some(field =>
                    field.toLowerCase().includes(searchText.toLowerCase())
                )
            )
    );
    let enableRemoveProfile = $derived<boolean>(profiles.length > 1);
    let enableClearSearch = $derived<boolean>(searchText.length > 1);
    let libraryMods = $derived<Mod[]>(
        mods.filter((m) => !profileConfigs.some(([guid, _]) => guid === m.guid)),
    );
    let libraryEnabled = $derived<boolean>(searchText.length === 0);
    let allowReorder = $derived<boolean>(searchText.length === 0);

    let unlisten: () => void;

    $effect(() => {
        if (!currentProfile) return;

        switch (currentProfile.Version) {
            case "V1":
                profileConfigs = Object.entries((currentProfile as ProfileV1).Configs) as [UUID, Config][];
                break;
        }

        return () => {
            const record = Object.fromEntries(profileConfigs) as Record<UUID, Config>;
            switch (currentProfile.Version) {
                case "V1":
                    currentProfile.Configs = record;
                    break;
            }
        };
    });

    $effect(() => {
        for (const mod of mods) {
            if (iconPaths.has(mod.guid)) continue;
            mod.iconPath()
                .then(path => iconPaths.set(mod.guid, path ?? null))
                .catch(() => iconPaths.set(mod.guid, null));
        }
    });

    onMount(async () => {
        initPromise = init();

        unlisten = await getCurrentWebview().onDragDropEvent(async (e) => {
            switch (e.payload.type) {
                case "enter":
                    isDragging = true;
                    break;
                
                case "drop":
                    isDragging = false;
                    const validPaths = e.payload.paths.filter(p => p.endsWith(".zip") || p.endsWith(".7z") || p.endsWith(".rar"));
                    if (validPaths.length == 0) return;
                    if (validPaths.length == 1) {
                        await doAddMod(validPaths[0]);
                    } else {
                        await doAddMods(...validPaths);
                    }
                    break;
                
                case "leave":
                    isDragging = false;
                    break;
            }
        });
    });

    onDestroy(() => unlisten?.());

    async function init() {
        const [loadedMods, loadedConfig] = await Promise.all([
            getMods(),
            loadProfiles(),
            new Promise((r) => setTimeout(r, 1000)), // added to prevent flashing of loading indicator
        ]);

        mods = loadedMods;
        profiles = loadedConfig.Profiles;
        activeProfile = loadedConfig.Active;
    }

    function makeConfigForMod(mod: Mod): Config {
        if (!("Version" in mod.Manifest)) {
            return {
                For: "Legacy",
                Enabled: true,
                Selected: 0
            };
        } else if (mod.Manifest.Version === 1) {
            const len = mod.Manifest.Options?.length ?? 0;
            return {
                For: "V1",
                Enabled: true,
                Toggled: new Array(len).fill(true),
                Selected: new Array(len).fill(0)
            };
        } else if (mod.Manifest.Version === 2) {
            const len = mod.Manifest.Options?.length ?? 0;
            return {
                For: "V2",
                Enabled: true,
                Toggled: new Array(len).fill(true),
                Selected: new Array(len).fill(0)
            };
        } else {
            throw "Unknown manifest version!";
        }
    }

    async function doAddMod(filename: string) {
        const wait = new WaitPopup(t("pages.mods.popup.wait.add.message"));
        showPopup(wait);
        try {
            const mod = await addMod(filename);
            mods.push(mod);
        } catch(ex: unknown) {
            let message: string;
            if (ex instanceof Error) {
                message = ex.message;
            } else if (typeof ex === "string") {
                message = ex;
            } else {
                message = "Unknown error!";
            }
            showPopup(new ErrorPopup(t("pages.mods.popup.notification.add_error.message"), message));
        } finally {
            wait.close();
        }
    }

    async function doAddMods(...filenames: string[]) {
        const wait = new WaitPopup(t("pages.mods.popup.wait.add_multiple.message"));
        showPopup(wait);
        try {

        } catch(ex: unknown) {

        } finally {
            wait.close();
        }
    }

    async function onAddProfile() {
        const input = await showPopup(
            new InputPopup(
                t("pages.mods.popup.input.add_profile.placeholder"),
                false,
                3,
            ),
        );
        if (!input) return;
    }

    async function onRemoveProfile() {
        const confirm = await showPopup(
            new ConfirmPopup(
                t("pages.mods.popup.confirm.remove_profile.title"),
                t("pages.mods.popup.confirm.remove_profile.question"),
            ),
        );
        if (!confirm) return;
    }

    function onDragStart(i: number) {
        if (!allowReorder) return;
        dragIndex = i;
    }

    function onDragOver(e: DragEvent, i: number) {
        e.preventDefault();

        if (!allowReorder) return;
        if (dragIndex === null || dragIndex === i) return;

        const reorderd = [...profileConfigs];
        const [removed] = reorderd.splice(dragIndex, 1);
        reorderd.splice(i, 0, removed);

        profileConfigs = reorderd;
        dragIndex = i;
    }

    function onDragEnd() {
        dragIndex = null;
    }

    async function onToggleLibrary() {
        libraryExtended = !libraryExtended;

        if (libraryExtended) {
            await new Promise((r) => setTimeout(r, 150));
            libraryVisible = true;
        } else {
            libraryVisible = false;
        }
    }

    function insertTop(i: number) {
        const mod = libraryMods[i];
        const config = makeConfigForMod(mod);
        profileConfigs.splice(0, 0, [mod.guid, config]);
    }

    function insertBottom(i: number) {
        const mod = libraryMods[i];
        const config = makeConfigForMod(mod);
        profileConfigs.push([mod.guid, config]);
    }

    async function onAddMod() {
        const filename = await open({
            multiple: false,
            directory: false,
            filters: [
                {
                    name: "Archives",
                    extensions: ["zip", "7z", "rar"]
                }
            ]
        });
        if (!filename) return;

        await doAddMod(filename);
    }

    async function onPurge() {
        const confirm = await showPopup(
            new ConfirmPopup(
                t("pages.mods.popup.confirm.purge.title"),
                t("pages.mods.popup.confirm.purge.question"),
            ),
        );
        if (!confirm) return;

        const wait = new WaitPopup(t("pages.mods.popup.wait.purge.message"));
        showPopup(wait);
        try {
            await new Promise((r) => setTimeout(r, 3000));
        } finally {
            wait.close();
        }
    }

    async function onDeploy() {
        if (!currentProfile) return;

        const wait = new WaitPopup("");
        showPopup(wait);
        try {
            const record = Object.fromEntries(profileConfigs) as Record<UUID, Config>;
            switch (currentProfile.Version) {
                case "V1":
                    currentProfile.Configs = record;
                    break;
            }
            await saveProfiles({ Profiles: profiles, Active: activeProfile });
        } finally {
            wait.close();
        }
    }

    function onLaunch() {
        openUrl("steam://launch/553850");
    }
</script>

{#await initPromise}
    <div class="w-full h-full flex justify-center items-center">
        <div
            class="p-4 bg-zinc-800 border-2 border-zinc-500 flex flex-col gap-1 items-center"
        >
            <div
                class="w-6 h-6 rounded-full border-4 border-transparent border-b-yellow-300 animate-spin"
            ></div>
            <span class="text-zinc-300">{t("pages.mods.loading.text")}</span>
        </div>
    </div>
{:then _}
    <div class="w-full h-full flex flex-col gap-1 relative">
        <div class="flex flex-row gap-1">
            <!-- Profiles -->
            <select class="flex-1 hd2mm-select" bind:value={activeProfile}>
                {#each profiles as profile, i}
                    <option value={i}>
                        {#if profile.Version === "V1"}
                            {profile.Name}
                        {/if}
                    </option>
                {/each}
            </select>
            <button
                class="hd2mm-button"
                title={t("pages.mods.add_profile_button.tip")}
                onclick={onAddProfile}
            >
                <Plus width="24" height="24" />
            </button>
            <button
                class="hd2mm-button"
                title={t("pages.mods.remove_profile_button.tip")}
                onclick={onRemoveProfile}
                disabled={!enableRemoveProfile}
            >
                <Dash width="24" height="24" />
            </button>
        </div>
        <!-- Search -->
        <div class="flex flex-row gap-1">
            <input
                class="flex-1 hd2mm-input"
                type="text"
                placeholder={t("pages.mods.search_input.placeholder")}
                bind:value={searchText}
            />
            <button
                class="hd2mm-button"
                title={t("pages.mods.clear_search_button.tip")}
                onclick={() => (searchText = "")}
                disabled={!enableClearSearch}
            >
                <Backspace width="24" height="24" />
            </button>
        </div>
        <!-- Center -->
        <div class="flex-1 flex flex-row relative">
            <!-- Mod List -->
            <ol class="flex-1 mr-7 overflow-y-scroll">
                {#each profileEntries as [guid, config, mod], i (guid)}
                    {@const iconPath = iconPaths.get(guid)}
                    <li
                        draggable={allowReorder}
                        ondragstart={() => onDragStart(i)}
                        ondragover={(e) => onDragOver(e, i)}
                        ondragend={onDragEnd}
                        class="mb-1 mr-1 p-2 text-zinc-300 bg-zinc-800 rounded flex flex-row gap-1 items-center"
                        class:cursor-grab={allowReorder}
                        class:opacity-50={dragIndex === i}
                    >
                        <img
                            class="w-14 h-14"
                            src={iconPath ?? "images/hd2_icon.png"}
                            alt={iconPath ? "Mod icon" : "Default icon"}
                        />
                        <div class="flex-1 flex flex-col gap-0.5 justify-between min-w-0">
                            <span class="text-2xl truncate">{mod.name}</span>
                            {#if "Version" in mod.Manifest && mod.Manifest.Version === 2 && mod.Manifest.Tags}
                                <div class="flex flex-row gap-1 overflow-hidden">
                                    {#each mod.Manifest.Tags as tag}
                                        <span class="px-1 bg-zinc-700 text-xs rounded">{tag}</span>
                                    {/each}
                                </div>
                            {/if}
                            <span class="text-sm truncate">{mod.description}</span>
                        </div>
                        <ToggleSwitch bind:checked={config.Enabled} />
                        {#if config.For === "Legacy"}
                            {#if !("Version" in mod.Manifest) && mod.Manifest.Options}
                                <select class="w-32 hd2mm-select" bind:value={config.Selected}>
                                    {#each mod.Manifest.Options as option, i }
                                        <option value={i}>{option}</option>
                                    {/each}
                                </select>
                            {/if}
                        {:else}
                            <button class="hd2mm-button-nop p-2">
                                <PencilSquare class="block mx-auto" />
                            </button>
                        {/if}
                    </li>
                {/each}
            </ol>
            <!-- Library -->
            <div
                class="flex flex-row gap-1 absolute z-10 transition-all right-0 h-full bg-zinc-900
                    {libraryExtended
                    ? 'w-70'
                    : 'w-6'}"
            >
                <button
                    class="hd2mm-button-nop w-6"
                    title={t("pages.mods.library_button.tip")}
                    onclick={onToggleLibrary}
                    disabled={!libraryEnabled}
                >
                    {#if libraryExtended}
                        <ArrowBarRight class="block m-auto" width="16" height="16" />
                    {:else}
                        <ArrowBarLeft class="block m-auto" width="16" height="16" />
                    {/if}
                </button>
                {#if libraryVisible}
                    <ul class="py-1 flex-1 border-y border-zinc-500 overflow-y-auto">
                        {#each libraryMods as mod, i}
                            <li class="grid grid-cols-[min-content_1fr] gap-1 text-zinc-300 bg-zinc-800 rounded mb-1 p-1">
                                <button
                                    class="hd2mm-button-nop p-1"
                                    onclick={() => insertTop(i)}
                                >
                                    <Arrow90degLeft class="block m-auto" width="16" height="16" />
                                </button>
                                <span class="text-2xl truncate">{mod.name}</span>
                                <button
                                    class="hd2mm-button-nop p-1"
                                    onclick={() => insertBottom(i)}
                                >
                                    <ArrowReturnLeft class="block m-auto" width="16" height="16" />
                                </button>
                                <span class="text-sm truncate">{mod.description}</span>
                            </li>
                        {/each}
                    </ul>
                {/if}
            </div>
        </div>
        <!-- Buttons -->
        <div class="flex flex-row gap-1">
            <button
                class="hd2mm-button"
                title={t("pages.mods.add_button.tip")}
                onclick={onAddMod}
            >
                {t("pages.mods.add_button.text")}
            </button>
            <div class="flex-1"></div>
            <button
                class="hd2mm-danger-button"
                title={t("pages.mods.purge_button.tip")}
                onclick={onPurge}
            >
                {t("pages.mods.purge_button.text")}
            </button>
            <button
                class="hd2mm-success-button"
                title={t("pages.mods.deploy_button.tip")}
                onclick={onDeploy}
            >
                {t("pages.mods.deploy_button.text")}
            </button>
            <button
                class="hd2mm-button"
                title={t("pages.mods.launch_button.tip")}
                onclick={onLaunch}
            >
                {t("pages.mods.launch_button.text")}
            </button>
        </div>
        <!--Drag overlay-->
        {#if isDragging}
            <div class="absolute z-10 bg-black/30 top-0 left-0 bottom-0 right-0 flex flex-col items-center justify-center">
                <span class="text-2xl text-zinc-300">{t("pages.mods.drop_message")}</span>
                <Download class="text-zinc-300" width="32" height="32" />
            </div>
        {/if}
    </div>
{:catch ex}
    <div class="w-full h-full flex justify-center items-center">
        <div class="p-4 bg-zinc-800 border-2 border-zinc-500 flex flex-col">
            <span class="text-red-500 text-xl self-center">
                {t("pages.mods.loading_failed.title")}
            </span>
            <p class="text-zinc-300 text-sm font-mono">{ex.toString()}</p>
        </div>
    </div>
{/await}
