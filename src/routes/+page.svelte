<script lang="ts">
    import { SvelteMap } from "svelte/reactivity";
    import { Plus, Dash, Backspace, ArrowBarRight, ArrowBarLeft, Arrow90degLeft, ArrowReturnLeft, PencilSquare, Download, ThreeDotsVertical, CaretUpFill, CaretDownFill, Trash3, ArrowBarUp, ArrowBarDown, CaretUp, CaretDown, Eraser } from "svelte-bootstrap-icons";
    import { openUrl } from "@tauri-apps/plugin-opener";
    import { open } from "@tauri-apps/plugin-dialog";
    import * as log from "@tauri-apps/plugin-log";
    import { SortableList } from "@rodrigodagostino/svelte-sortable-list"
    import { useLocalization } from "$lib/state/localization.svelte";
    import type { Mod } from "$lib/models/mod";
    import type { Config, Profile } from "$lib/models/profile";
    import { addMod, addMods, deleteMod, getMods, loadProfiles, saveProfiles, deploy, purge } from "$lib/utils/commands";
    import type { UUID } from "$lib/types/uuid";
    import { usePopup } from "$lib/state/popup.svelte";
    import {
        ConfirmPopup,
        InputPopup,
        WaitPopup,
        NotificationPopup,
        ErrorPopup,
        AddResultPopup
    } from "$lib/types/popup";
    import ToggleSwitch from "$lib/components/ToggleSwitch.svelte";
    import PopupMenuButton from "$lib/components/PopupMenuButton.svelte";
    import { onNavigate } from "$app/navigation";
    import type { ModAddResult } from "$lib/types/results";

    const { t } = useLocalization();
    const { show: showPopup } = usePopup();

    let mods = $state<Mod[]>([]);
    let profiles = $state<Profile[]>([]);
    let activeProfile = $state<number>(0);
    let searchText = $state<string>("");
    let profileConfigs = $state<Config[]>([]);
    let iconPaths = new SvelteMap<UUID, string | null>();
    let libraryExtended = $state<boolean>(false);
    let libraryVisible = $state<boolean>(false);
    let isDragging = $state<boolean>(false);
    let initPromise = $state<Promise<void>>(init());

    let currentProfile = $derived<Profile | undefined>(profiles[activeProfile]);
    let profileMods = $derived<Mod[]>(profileConfigs.map(config => mods.find(m => m.guid === config.Guid)).filter((m): m is Mod => m !== undefined));
    let profileEntries = $derived<[Config, Mod][]>(
        profileConfigs
            .map((config, i) => [config, profileMods[i]] as [Config, Mod])
            .filter(([_, mod]) =>
                searchText.length === 0 ||
                [mod.name, mod.description].some(field =>
                    field.toLowerCase().includes(searchText.toLowerCase())
                )
            )
    );
    let enableRemoveProfile = $derived<boolean>(profiles.length > 1);
    let enableClearSearch = $derived<boolean>(searchText.length > 1);
    let libraryMods = $derived<Mod[]>(
        mods.filter((m) => !profileConfigs.some(config => config.Guid === m.guid)),
    );
    let libraryEnabled = $derived<boolean>(searchText.length === 0);
    let allowReorder = $derived<boolean>(searchText.length === 0);
    
    $effect(() => {
        if (!currentProfile) return;

        switch (currentProfile.Version) {
            case "V1":
                profileConfigs = currentProfile.Configs;
                break;
        }

        return applyCurrentConfigChanges;
    });

    $effect(() => {
        for (const mod of mods) {
            if (iconPaths.has(mod.guid)) continue;
            mod.iconPath()
                .then(path => iconPaths.set(mod.guid, path ?? null))
                .catch(() => iconPaths.set(mod.guid, null));
        }
    });

    onNavigate(async () => {
        await doSaveProfiles();
    });

    async function init() {
        const [loadedMods, loadedConfig] = await Promise.all([
            getMods(),
            loadProfiles()
        ]);
        
        mods = loadedMods;
        profiles = loadedConfig.Profiles;
        activeProfile = loadedConfig.Active;
    }

    function applyCurrentConfigChanges() {
        switch (currentProfile!.Version) {
            case "V1":
                currentProfile!.Configs = profileConfigs;
                break;
        }
    }

    function removeModFromProfiles(mod: Mod) {
        profiles.forEach(profile => {
            switch (profile.Version) {
                case "V1":
                    const i = profile.Configs.findIndex(config => config.Guid === mod.guid);
                    if (i === -1) return;
                    profile.Configs.splice(i, 1);
                    break;
            }
        });
    }

    function makeConfigForMod(mod: Mod): Config {
        if (!("Version" in mod.Manifest)) {
            return {
                For: "Legacy",
                Guid: mod.Manifest.Guid,
                Enabled: true,
                Selected: 0
            };
        } else if (mod.Manifest.Version === 1) {
            const len = mod.Manifest.Options?.length ?? 0;
            return {
                For: "V1",
                Guid: mod.Manifest.Guid,
                Enabled: true,
                Toggled: new Array(len).fill(true),
                Selected: new Array(len).fill(0)
            };
        } else if (mod.Manifest.Version === 2) {
            const len = mod.Manifest.Options?.length ?? 0;
            return {
                For: "V2",
                Guid: mod.Manifest.Guid,
                Enabled: true,
                Toggled: new Array(len).fill(true),
                Selected: new Array(len).fill(0)
            };
        } else {
            throw "Unknown manifest version!";
        }
    }

    async function doDeleteMod(guid: string) {
        const i = mods.findIndex(m => m.guid == guid);
        if (i === -1) return;

        const wait = new WaitPopup(t("pages.mods.popup.wait.delete.message"));
        showPopup(wait);

        try {
            const [mod] = mods.splice(i, 1);
            removeModFromProfiles(mod);
            await deleteMod(mod.guid);
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
            showPopup(new ErrorPopup(t("pages.mods.popup.error.add.message"), message));
        } finally {
            wait.close();
        }
    }

    async function doAddMods(...filenames: string[]) {
        const wait = new WaitPopup(t("pages.mods.popup.wait.add_multiple.message"));
        showPopup(wait);

        try {
            const results = await addMods(filenames);

            const addResults = results.map<ModAddResult>((r, i) => {
                if ("Ok" in r) {
                    return {
                        success: true,
                        archiveFile: filenames[i]
                    };
                } else {
                    return {
                        success: false,
                        archiveFile: filenames[i],
                        errorMessage: r.Err
                    }
                }
            });
            const popup = new AddResultPopup(addResults);
            showPopup(popup)
            
            const modsToAdd = results.filter(r => "Ok" in r).map(r => r.Ok);
            mods.push(...modsToAdd);
        } catch(ex: unknown) {
            let message: string;
            if (ex instanceof Error) {
                message = ex.message;
            } else if (typeof ex === "string") {
                message = ex;
            } else {
                message = "Unknown error!";
            }
            showPopup(new ErrorPopup(t("pages.mods.popup.error.add.message"), message));
        } finally {
            wait.close();
        }
    }

    async function doSaveProfiles(): Promise<boolean> {
        const wait = new WaitPopup(t("pages.mods.popup.wait.saving.message"));
        showPopup(wait);

        try {
            applyCurrentConfigChanges()
            await saveProfiles({ Profiles: profiles, Active: activeProfile });
            return true;
        } catch {
            return false;
        } finally {
            wait.close();
        }
    }

    async function onAddProfile() {
        const input = await showPopup(new InputPopup(
            t("pages.mods.popup.input.add_profile.placeholder"),
            false,
            3,
        ));
        if (!input) return;
        
        profiles.push({
            Version: "V1",
            Name: input,
            Configs: []
        });
        activeProfile = profiles.length - 1;
    }

    async function onRemoveProfile() {
        const confirm = await showPopup(
            new ConfirmPopup(
                t("pages.mods.popup.confirm.remove_profile.title"),
                t("pages.mods.popup.confirm.remove_profile.question"),
            ),
        );
        if (!confirm) return;

        profiles.splice(activeProfile, 1);
        if (activeProfile > 0) activeProfile--;
    }

    function onDragEnd(e: SortableList.RootEvents["ondragend"]) {
        const { draggedItemIndex, targetItemIndex, isCanceled } = e;

        if (isCanceled || typeof targetItemIndex !== "number" || draggedItemIndex === targetItemIndex) return;

        const [elm] = profileConfigs.splice(draggedItemIndex, 1);
        profileConfigs.splice(targetItemIndex, 0, elm);
    }

    async function onEditConfig(i: number) {

    }

    function onRemove(i: number) {
        profileConfigs.splice(i, 1);
    }

    function onMoveUp(i: number) {
        if (i === 0) return;

        const [elm] = profileConfigs.splice(i, 1);
        profileConfigs.splice(i - 1, 0, elm);
    }

    function onMoveDown(i: number) {
        if (i === profileConfigs.length - 1) return;

        const [elm] = profileConfigs.splice(i, 1);
        profileConfigs.splice(i + 1, 0, elm);
    }

    function onToTop(i: number) {
        if (i === 0) return;

        const [elm] = profileConfigs.splice(i, 1);
        profileConfigs.splice(0, 0, elm);
    }

    function onToBottom(i: number) {
        if (i === profileConfigs.length - 1) return;

        const [elm] = profileConfigs.splice(i, 1);
        profileConfigs.push(elm);
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

    function onInsertTop(i: number) {
        const mod = libraryMods[i];
        const config = makeConfigForMod(mod);
        profileConfigs.splice(0, 0, config);
    }

    function onInsertBottom(i: number) {
        const mod = libraryMods[i];
        const config = makeConfigForMod(mod);
        profileConfigs.push(config);
    }

    async function onDelete(i: number) {
        const confirm = new ConfirmPopup(
            t("pages.mods.popup.confirm.delete.title"),
            t("pages.mods.popup.confirm.delete.question"),
        );
        if (!await showPopup(confirm)) return;
        const mod = libraryMods[i];
        await doDeleteMod(mod.guid);
    }

    function onUpdate(i: number) {
        const mod = libraryMods[i];
        //TODO
    }

    async function onAddMod() {
        const filenames = await open({
            multiple: true,
            directory: false,
            filters: [
                {
                    name: "Archives",
                    extensions: ["zip", "7z", "rar"]
                }
            ]
        });
        if (!filenames) return;

        if (filenames.length == 1) {
            await doAddMod(filenames[0]);
        } else {
            await doAddMods(...filenames);
        }
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
            await purge();
            showPopup(new NotificationPopup("info", t("pages.mods.popup.notification.purge_success.message")));
        } catch(ex: unknown) {
            let message: string;
            if (ex instanceof Error) {
                message = ex.message;
            } else if (typeof ex === "string") {
                message = ex;
            } else {
                message = "Unknown error!";
            }
            showPopup(new ErrorPopup(t("pages.mods.popup.error.purge.message"), message));
        } finally {
            wait.close();
        }
    }

    async function onDeploy() {
        if (!currentProfile) return;
        
        if (currentProfile.Configs.length === 0) {
            showPopup(new NotificationPopup("error", t("pages.mods.popup.notification.empty_deploy_error.message")));
            return;
        }

        const wait = new WaitPopup(t("pages.mods.popup.wait.deploy.message"));
        showPopup(wait);

        try {
            await deploy(currentProfile.Configs);
            showPopup(new NotificationPopup("info", t("pages.mods.popup.notification.deploy_success.message")));
        } catch(ex: unknown) {
            let message: string;
            if (ex instanceof Error) {
                message = ex.message;
            } else if (typeof ex === "string") {
                message = ex;
            } else {
                message = "Unknown error!";
            }
            showPopup(new ErrorPopup(t("pages.mods.popup.error.deploy.message"), message));
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
                autocomplete="off"
                autocorrect="off"
                autocapitalize="off"
                spellcheck="false"
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
            <div class="flex-1 mr-7 pr-1 overflow-y-scroll overflow-x-hidden">
                <SortableList.Root
                    ondragend={onDragEnd}
                    isDisabled={!allowReorder}
                    gap={4}
                >
                    {#each profileEntries as [config, mod], i (config.Guid)}
                        {@const iconPath = iconPaths.get(config.Guid)}
                        <SortableList.Item
                            id={config.Guid}
                            index={i}
                        >
                            <div class="p-2 text-zinc-300 bg-zinc-800 rounded flex flex-row gap-1 items-center">
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
                                    <button
                                        class="hd2mm-button-nop p-2"
                                        onclick={() => onEditConfig(i)}
                                    >
                                        <PencilSquare class="block mx-auto" />
                                    </button>
                                {/if}
                                <PopupMenuButton insertTarget="main">
                                    <button onclick={() => onRemove(i)}>
                                        <Eraser />
                                        <span>Remove</span>
                                    </button>
                                    <hr>
                                    <button
                                        disabled={i === 0}
                                        onclick={() => onMoveUp(i)}
                                    >
                                        <CaretUp />
                                        <span>Move Up</span>
                                    </button>
                                    <button
                                        disabled={i === profileEntries.length - 1}
                                        onclick={() => onMoveDown(i)}
                                    >
                                        <CaretDown />
                                        <span>Move Down</span>
                                    </button>
                                    <button
                                        disabled={i === 0}
                                        onclick={() => onToTop(i)}
                                    >
                                        <ArrowBarUp />
                                        <span>To Top</span>
                                    </button>
                                    <button
                                        disabled={i === profileEntries.length - 1}
                                        onclick={() => onToBottom(i)}
                                    >
                                        <ArrowBarDown />
                                        <span>To Bottom</span>
                                    </button>
                                </PopupMenuButton>
                            </div>
                        </SortableList.Item>
                    {/each}
                </SortableList.Root>
            </div>
            <!-- Library -->
            <div
                class="flex flex-row gap-1 absolute z-10 transition-all right-0 h-full bg-zinc-900
                    {libraryExtended
                    ? 'w-90'
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
                            <li class="grid grid-cols-[min-content_1fr_min-content] gap-1 text-zinc-300 bg-zinc-800 rounded mb-1 p-1">
                                <button
                                    class="hd2mm-button-nop p-1"
                                    title={t("pages.mods.library.insert_top_button.tip")}
                                    onclick={() => onInsertTop(i)}
                                >
                                    <Arrow90degLeft class="block m-auto" width="16" height="16" />
                                </button>
                                <span class="text-2xl truncate">{mod.name}</span>
                                <button
                                    class="hd2mm-button-nop p-1"
                                    title={t("pages.mods.library.delete_button.tip")}
                                    onclick={() => onDelete(i)}
                                >
                                    <Trash3 class="block m-auto" width="16" height="16" />
                                </button>
                                <button
                                    class="hd2mm-button-nop p-1"
                                    title={t("pages.mods.library.insert_bottom_button.tip")}
                                    onclick={() => onInsertBottom(i)}
                                >
                                    <ArrowReturnLeft class="block m-auto" width="16" height="16" />
                                </button>
                                <span class="text-sm truncate">{mod.description}</span>
                                <button
                                    class="hd2mm-button-nop p-1"
                                    title={t("pages.mods.library.update_button.tip")}
                                    onclick={() => onUpdate(i)}
                                    disabled
                                >
                                    <Download class="block m-auto" width="16" height="16" />
                                </button>
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
