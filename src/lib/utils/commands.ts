import { invoke } from '@tauri-apps/api/core';
import { Mod } from '../models/mod';
import type { ProfilesConfig } from '$lib/models/profile';
import type { Manifest } from '$lib/models/manifest';

export async function getMods(): Promise<Mod[]> {
    const mods = await invoke<{ Manifest: Manifest, Directory: string }[]>("get_mods");
    return mods.map(m => new Mod(m.Manifest, m.Directory));
}

export async function addMod(archiveFile: string): Promise<Mod> {
    const mod = await invoke<{ Manifest: Manifest, Directory: string }>("add_mod", { "archive_file": archiveFile });
    return new Mod(mod.Manifest, mod.Directory);
}

export async function addMods(archiveFiles: string[]): Promise<Mod[]> {
    const mods = await invoke<{ Manifest: Manifest, Directory: string }[]>("add_mods", { "archive_files": archiveFiles });
    return mods.map(mod => new Mod(mod.Manifest, mod.Directory));
}

export async function loadProfiles(): Promise<ProfilesConfig> {
    return await invoke<ProfilesConfig>("load_profiles");
}

export async function saveProfiles(config: ProfilesConfig): Promise<void> {
    await invoke<void>("save_profiles", { config });
}