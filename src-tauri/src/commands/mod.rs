use std::{collections::{HashMap, HashSet}, path::{Path, PathBuf}, sync::OnceLock};

use anyhow_tauri::{IntoTAResult, TAResult};
use regex::Regex;
use tauri::State;

use crate::{AppState, commands::settings::{do_check_settings, do_load_settings, load_settings}, models::{manifest::Manifest, profile::Config}};

pub mod mods;
pub mod profiles;
pub mod settings;

static PATCH_REGEX: OnceLock<Regex> = OnceLock::new();
static INDEX_REGEX: OnceLock<Regex> = OnceLock::new();

struct PatchFileTriplet {
    patch: Option<PathBuf>,
    gpu_resources: Option<PathBuf>,
    stream: Option<PathBuf>,
}

async fn get_patch_files_from_dir(dir: &Path) -> anyhow::Result<Vec<PathBuf>> {
    let patch_regex = PATCH_REGEX.get_or_init(|| Regex::new(r"^[0-9a-f]{16}\.patch_\d+(?:\.gpu_resources|\.stream)?$").unwrap());

    let mut entries = Vec::new();
    let mut dir_reader = tokio::fs::read_dir(dir).await?;
    while let Some(entry) = dir_reader.next_entry().await? {
        if !entry.file_type()
            .await
            .map(|t| t.is_file())
            .unwrap_or(false) {
            continue;
        }
        let path = entry.path();
        if !path.file_name()
            .and_then(|n| n.to_str())
            .map(|n| patch_regex.is_match(n))
            .unwrap_or(false) {
            continue;
        }
        entries.push(path);
    }

    Ok(entries)
}

async fn add_files_from_dir(dir: &Path, groups: &mut HashMap<String, Vec<PatchFileTriplet>>) -> anyhow::Result<()> {
    let index_regex = INDEX_REGEX.get_or_init(|| Regex::new(r"\.patch_(\d+)").unwrap());

    let entries = get_patch_files_from_dir(dir).await?;

    let names: HashSet<String> = entries
        .iter()
        .filter_map(|p| p.file_name()?.to_str().map(|s| s[..16].to_string()))
        .collect();

    for name in names {
        let indices: HashSet<u32> = entries
            .iter()
            .filter_map(|p| {
                let fname = p.file_name()?.to_str()?;
                if !fname.starts_with(&*name) {
                    return None;
                }
                let caps = index_regex.captures(fname)?;
                caps[1].parse().ok()
            })
            .collect();

        for index in indices {
            let patch = entries.iter().find(|p| {
                p.file_name()
                    .and_then(|n| n.to_str())
                    .map(|n| n == format!("{}.patch_{}", name, index))
                    .unwrap_or(false)
            }).cloned();

            let gpu_resources = entries.iter().find(|p| {
                p.file_name()
                    .and_then(|n| n.to_str())
                    .map(|n| n == format!("{}.patch_{}.gpu_resources", name, index))
                    .unwrap_or(false)
            }).cloned();

            let stream = entries.iter().find(|p| {
                p.file_name()
                    .and_then(|n| n.to_str())
                    .map(|n| n == format!("{}.patch_{}.stream", name, index))
                    .unwrap_or(false)
            }).cloned();

            groups
                .entry(name.clone())
                .or_default()
                .push(PatchFileTriplet { patch, gpu_resources, stream });
        }
    }

    Ok(())
}

async fn do_purge(data_dir: &Path) -> anyhow::Result<()> {
    let patch_files = get_patch_files_from_dir(data_dir).await?;

    futures::future::try_join_all(patch_files.iter().map(|f| tokio::fs::remove_file(f))).await?;

    Ok(())
}

#[tauri::command]
pub async fn deploy(state: State<'_, AppState>, configs: Vec<Config>) -> TAResult<()> {
    let mods = state.inner().mods.lock().await;
    if mods.is_none() {
        return anyhow::anyhow!("mods not read").into_ta_result();
    }
    let mods = mods.as_ref().unwrap();

    let settings = do_load_settings(&state.base_path).await?;
    if let Err(e) = settings.validate().await {
        return anyhow::anyhow!("invalid settings: {}", e).into_ta_result();
    }

    let mods = mods.iter()
        .map(|m| (m.guid(), m))
        .collect::<HashMap<_, _>>();
    let mods = configs.iter()
        .filter_map(|c| {
            mods.get(c.uuid()).map(|m| (*m, c))
        })
        .collect::<Vec<_>>();

    let data_dir = settings.game_path().join("data");

    do_purge(&data_dir).await?;

    if mods.is_empty() {
        return Ok(());
    }

    let mut groups: HashMap<String, Vec<PatchFileTriplet>> = HashMap::new();

    for (r#mod, config) in mods {
        if !config.enabled() {
            continue;
        }

        match (&r#mod.manifest, config) {
            (Manifest::Legacy(manifest), Config::Legacy { selected, .. }) => {
                let base = &r#mod.directory;

                if let Some(options) = manifest.options.as_ref() {
                    if let Some(opt) = options.get(*selected) {
                        let dir = base.join(opt);
                        add_files_from_dir(&dir, &mut groups).await?;
                    }
                } else {
                    add_files_from_dir(base, &mut groups).await?;
                }
            }
            (Manifest::V1(manifest), Config::V1 { selected, toggled, .. }) => {
                let base = &r#mod.directory;

                if let Some(options) = manifest.options.as_ref() {
                    for (i, opt) in options.iter().enumerate() {
                        if !toggled.get(i).copied().unwrap_or(false) {
                            continue;
                        }

                        if let Some(includes) = opt.include.as_ref() {
                            for inc in includes {
                                let dir = base.join(inc);
                                add_files_from_dir(&dir, &mut groups).await?;
                            }
                        }

                        if let Some(sub_options) = opt.sub_options.as_ref() {
                            if let Some(idx) = selected.get(i).cloned() {
                                if let Some(sub) = sub_options.get(idx) {
                                    for inc in &sub.include {
                                        let dir = base.join(inc);
                                        add_files_from_dir(&dir, &mut groups).await?;
                                    }
                                }
                            }
                        }
                    }
                } else {
                    add_files_from_dir(base, &mut groups).await?;
                }
            }
            (Manifest::V2(manifest), Config::V2 { selected, toggled, .. }) => {
                todo!("V2 manifest mods not supported yet");
            }
            _ => unreachable!("manifest and config version should always match")
        }
    }
    
    for (name, triplets) in &groups {
        let offset = if settings.has_skip_entry(name) { 1 } else { 0 };

        for (i, triplet) in triplets.iter().enumerate() {
            let index = i + offset;

            let patch_dest = data_dir.join(format!("{}.patch_{}", name, index));
            match &triplet.patch {
                Some(src) => { tokio::fs::copy(src, &patch_dest).await.into_ta_result()?; }
                None => { tokio::fs::File::create(&patch_dest).await.into_ta_result()?; }
            }
            
            let gpu_dest = data_dir.join(format!("{}.patch_{}.gpu_resources", name, index));
            match &triplet.gpu_resources {
                Some(src) => { tokio::fs::copy(src, &gpu_dest).await.into_ta_result()?; }
                None => { tokio::fs::File::create(&gpu_dest).await.into_ta_result()?; }
            }
            
            let stream_dest = data_dir.join(format!("{}.patch_{}.stream", name, index));
            match &triplet.stream {
                Some(src) => { tokio::fs::copy(src, &stream_dest).await.into_ta_result()?; }
                None => { tokio::fs::File::create(&stream_dest).await.into_ta_result()?; }
            }
        }
    }

    Ok(())
}

#[tauri::command]
pub async fn purge(state: State<'_, AppState>) -> TAResult<()> {
    let settings = load_settings(state).await?;
    if let Err(e) = settings.validate().await {
        return anyhow::anyhow!("invalid settings: {}", e).into_ta_result();
    }

    let data_dir = settings.game_path().join("data");
    do_purge(&data_dir).await.into_ta_result()
}