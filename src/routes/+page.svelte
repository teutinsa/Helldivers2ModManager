<script lang="ts">
    import { Plus, Dash, Backspace } from "svelte-bootstrap-icons";
    import { useLocalization } from "$lib/state/localization.svelte";
    import { createLogger } from "$lib/utils/logger";
    import type { Mod } from "$lib/models/mod";
    import type { Config, Profile, ProfileV1 } from "$lib/models/profile";
    import { getMods, loadProfiles, saveProfiles } from "$lib/utils/commands";
    import type { UUID } from "$lib/types/uuid";
    import { onMount } from "svelte";
    import { usePopup } from "$lib/state/popup.svelte";
    import { ConfirmPopup, InputPopup } from "$lib/types/popup";
    import { openUrl } from "@tauri-apps/plugin-opener";

    const log = createLogger("ModsPage");
    const { t } = useLocalization();
    const { show: showPopup } = usePopup();

    let mods = $state<Mod[]>([]);
    let profiles = $state<Profile[]>([]);
    let activeProfile = $state<number>(0);
    let searchText = $state<string>("");
    let profileMods = $state<[UUID, Config][]>([]);
    let initPromise = $state<Promise<void>>();

    let currentProfile = $derived<Profile | undefined>(profiles[activeProfile]);
    let libraryMods = $derived<Mod[]>(
        mods.filter((m) => !profileMods.some(([guid, _]) => guid === m.guid)),
    );

    $effect(() => {
        if (!currentProfile) return;

        switch (currentProfile.Version) {
            case "V1":
                profileMods = Object.entries(
                    (currentProfile as ProfileV1).Configs,
                ) as [UUID, Config][];
                break;
        }
    });

    onMount(() => {
        initPromise = init();
    });

    async function init() {
        mods = await getMods();
        const config = await loadProfiles();
        profiles = config.Profiles;
        activeProfile = config.Active;
    }

    async function addProfile() {
        const input = await showPopup(new InputPopup(
            t("pages.mods.popup.input.add_profile.placeholder")
        ));
    }

    async function removeProfile() {
        const confirm = await showPopup(new ConfirmPopup(
            t("pages.mods.popup.confirm.remove_profile.title"),
            t("pages.mods.popup.confirm.remove_profile.question")
        ));
        if (!confirm) return;
    }

    async function purge() {
        const confirm = await showPopup(new ConfirmPopup(
            t("pages.mods.popup.confirm.purge.title"),
            t("pages.mods.popup.confirm.purge.question")
        ));
        if (!confirm) return;
    }

    async function deploy() {
        if (!currentProfile) return;

        const record = Object.fromEntries(profileMods) as Record<UUID, Config>;
        switch (currentProfile.Version) {
            case "V1":
                (currentProfile as ProfileV1).Configs = record;
                break;
        }
        await saveProfiles({ Profiles: profiles, Active: activeProfile });
    }

    async function launch() {
        await openUrl("steam://launch/553850");
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
{:then}
    <!-- Profiles -->
    <div class="w-full h-full flex flex-col gap-1">
        <div class="flex flex-row gap-1">
            <select
                class="flex-1 px-2 py-1 text-zinc-300 border-zinc-500 border-2 hover:border-yellow-300 focus:outline-none focus:border-yellow-300"
                bind:value={activeProfile}
            >
                {#each profiles as profile, i}
                    <option
                        class="bg-zinc-800 text-zinc-300 hover:bg-yellow-300 hover:text-zinc-900"
                        value={i}
                    >
                        {#if profile.Version === "V1"}
                            {(profile as ProfileV1).Name}
                        {/if}
                    </option>
                {/each}
            </select>
            <button
                class="hd2mm-button"
                title={t("pages.mods.add_profile_button.tip")}
                onclick={addProfile}
            >
                <Plus width="24" height="24" />
            </button>
            <button
                class="hd2mm-button"
                title={t("pages.mods.remove_profile_button.tip")}
                onclick={removeProfile}
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
            >
                <Backspace width="24" height="24" />
            </button>
        </div>
        <!-- Center -->
        <div class="flex-1 flex flex-row">
            <!-- Mod List -->
            <ol class="flex-1">
                {#each mods as mod (mod.guid)}
                    <li>
                        {mod.name}
                    </li>
                {/each}
            </ol>
            <!-- Library -->
            <div></div>
        </div>
        <!-- Buttons -->
        <div class="flex flex-row gap-1 justify-end">
            <button
                class="hd2mm-danger-button"
                title={t("pages.mods.purge_button.tip")}
                onclick={purge}
            >
                {t("pages.mods.purge_button.text")}
            </button>
            <button
                class="hd2mm-success-button"
                title={t("pages.mods.deploy_button.tip")}
            >
                {t("pages.mods.deploy_button.text")}
            </button>
            <button
                class="hd2mm-button"
                title={t("pages.mods.launch_button.tip")}
            >
                {t("pages.mods.launch_button.text")}
            </button>
        </div>
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
