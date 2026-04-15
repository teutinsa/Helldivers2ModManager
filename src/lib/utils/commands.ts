import type { Path } from 'typescript';
import { invoke } from '@tauri-apps/api/core';
import type { Mod } from '../models/mod';
import type { ProfilesConfig } from '$lib/models/profile';

export async function getMods(): Promise<Mod[]> {
    return await invoke<Mod[]>("get_mods");
}

export async function addMod(archiveFile: Path): Promise<Mod> {
    return await invoke<Mod>("add_mod", { "archive_file": archiveFile });
}

export async function loadProfiles(): Promise<ProfilesConfig> {
    return await invoke<ProfilesConfig>("load_profiles");
}

export async function saveProfiles(config: ProfilesConfig): Promise<void> {
    await invoke<void>("save_profiles", { config });
}