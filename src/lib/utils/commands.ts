import { invoke } from '@tauri-apps/api/core';
import * as log from '@tauri-apps/plugin-log';
import { Mod } from '../models/mod';
import type { Config, ProfilesConfig } from '$lib/models/profile';
import type { Manifest } from '$lib/models/manifest';
import type { RustResult } from '$lib/types/results';
import type { UUID } from '$lib/types/uuid';
import type { Settings } from '$lib/models/settings';

export async function getMods(): Promise<Mod[]> {
    log.debug("Invoking `get_mods`.");
    const mods = await invoke<{ Manifest: Manifest, Directory: string }[]>("get_mods");
    return mods.map(m => new Mod(m.Manifest, m.Directory));
}

export async function deleteMod(guid: UUID): Promise<void> {
    log.debug("Invoking `delete_mod`.");
    await invoke<void>("delete_mod", { guid });
}

export async function addMod(archiveFile: string): Promise<Mod> {
    log.debug("Invoking `add_mod`.");
    const mod = await invoke<{ Manifest: Manifest, Directory: string }>("add_mod", { archiveFile });
    return new Mod(mod.Manifest, mod.Directory);
}

export async function addMods(archiveFiles: string[]): Promise<RustResult<Mod>[]> {
    log.debug("Invoking `add_mods`.");
    const results = await invoke<RustResult<{ Manifest: Manifest, Directory: string }>[]>("add_mods", { archiveFiles });
    return results.map(result => {
        if ("Ok" in result) {
            return {
                Ok: new Mod(result.Ok.Manifest, result.Ok.Directory)
            };
        }
        return result;
    });
}

export async function loadProfiles(): Promise<ProfilesConfig> {
    log.debug("Invoking `load_profiles`.");
    return await invoke<ProfilesConfig>("load_profiles");
}

export async function saveProfiles(config: ProfilesConfig): Promise<void> {
    log.debug("Invoking `save_profiles`.");
    await invoke<void>("save_profiles", { config });
}

export async function loadSettings(): Promise<Settings> {
    log.debug("Invoking `load_settings`.");
    return await invoke<Settings>("load_settings");
}

export async function saveSettings(settings: Settings): Promise<void> {
    log.debug("Invoking `save_settings`.");
    await invoke<void>("save_settings", { settings });
}

export async function checkSettings(): Promise<boolean> {
    log.debug("Invoking `check_settings`.");
    return await invoke<boolean>("check_settings");
}

export async function deploy(configs: Config[]): Promise<void> {
    log.debug("Invoking `deploy`.");
    await invoke<void>("deploy", { configs });
}

export async function purge(): Promise<void> {
    log.debug("Invoking `purge`.");
    await invoke<void>("purge");
}