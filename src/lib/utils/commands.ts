import { invoke } from '@tauri-apps/api/core';
import { Mod } from '../models/mod';
import type { ProfilesConfig } from '$lib/models/profile';
import type { Manifest } from '$lib/models/manifest';
import type { RustResult } from '$lib/types/results';
import { createLogger } from './logger';

const log = createLogger("Commands");

export async function getMods(): Promise<Mod[]> {
    const mods = await invoke<{ Manifest: Manifest, Directory: string }[]>("get_mods");
    return mods.map(m => new Mod(m.Manifest, m.Directory));
}

export async function addMod(archiveFile: string): Promise<Mod> {
    const mod = await invoke<{ Manifest: Manifest, Directory: string }>("add_mod", { archiveFile });
    return new Mod(mod.Manifest, mod.Directory);
}

export async function addMods(archiveFiles: string[]): Promise<RustResult<Mod>[]> {
    log.debug("Adding mods...", { archiveFiles });
    const results = await invoke<RustResult<{ Manifest: Manifest, Directory: string }>[]>("add_mods", { archiveFiles });
    log.debug("Added mods.", { results });
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
    return await invoke<ProfilesConfig>("load_profiles");
}

export async function saveProfiles(config: ProfilesConfig): Promise<void> {
    await invoke<void>("save_profiles", { config });
}