use crate::{
    archive::Archive,
    models::{
        manifest::{legacy, Manifest},
        Mod,
    },
    AppState,
};
use anyhow_tauri::{IntoTAResult, TAResult};
use rand::{rngs::SysRng, TryRng};
use std::{collections::HashSet, path::PathBuf};
use tauri::State;
use uuid::Uuid;

const MODS_DIRECTORY: &'static str = "mods/";
const MANIFEST_FILE: &'static str = "manifest.json";

#[allow(dead_code)]
trait ZipResult<S, E, T> {
    fn zip(self, other: Result<T, E>) -> Result<(S, T), E>;
    fn zip_value(self, other: T) -> Result<(S, T), E>;
}

impl<S, E, T> ZipResult<S, E, T> for Result<S, E> {
    fn zip(self, other: Result<T, E>) -> Result<(S, T), E> {
        match (self, other) {
            (Ok(a), Ok(b)) => Ok((a, b)),
            (Err(e), Ok(_)) => Err(e),
            (Ok(_), Err(e)) => Err(e),
            (Err(e), Err(_)) => Err(e),
        }
    }

    fn zip_value(self, other: T) -> Result<(S, T), E> {
        match self {
            Ok(s) => Ok((s, other)),
            Err(e) => Err(e)
        }
    }
}

#[tauri::command]
pub async fn get_mods(state: State<'_, AppState>) -> TAResult<Vec<Mod>> {
    let mut state_mods = state.mods.lock().await;

    log::info!("Loading mods...");

    if let Some(mods) = state_mods.as_ref() {
        log::info!("Mods already loaded.");
        return Ok(mods.clone());
    }

    let mods_dir = state.base_path.join(MODS_DIRECTORY);
    if !mods_dir.is_dir() {
        tokio::fs::create_dir(mods_dir).await.into_ta_result()?;
        return Ok(vec![]);
    }

    let mut mods = Vec::new();
    let mut mods_dir = tokio::fs::read_dir(mods_dir).await.into_ta_result()?;
    while let Some(entry) = mods_dir.next_entry().await.into_ta_result()? {
        let mod_dir = entry.path();

        let manifest_file = mod_dir.join(MANIFEST_FILE);
        if !manifest_file.is_file() {
            continue;
        }

        let manifest_data = tokio::fs::read(manifest_file).await.into_ta_result()?;
        let manifest: Manifest = serde_json::from_slice(&manifest_data).into_ta_result()?;

        let mut r#mod = Mod {
            manifest,
            directory: mod_dir,
        };
        r#mod.normalize_paths().await?;

        mods.push(r#mod);
    }

    *state_mods = Some(mods.clone());
    log::info!("Mods loaded.");
    Ok(mods)
}

#[tauri::command]
pub async fn delete_mod(state: State<'_, AppState>, guid: Uuid) -> TAResult<()> {
    let mut mods = state.mods.lock().await;
    if mods.is_none() {
        return anyhow::anyhow!("mods not read").into_ta_result();
    }
    let mods = mods.as_mut().unwrap();

    log::info!("Deleting mod \"{}\"...", guid);

    if let Some(i) = mods.iter().position(|m| m.guid() == guid) {
        let r#mod = mods.remove(i);
        log::info!("Mod removed form regitry.");

        log::info!("Deleting files...");
        tokio::fs::remove_dir_all(r#mod.directory).await.into_ta_result()?;
        
        log::info!("Mod deletion complete.");
        Ok(())
    } else {
        anyhow_tauri::bail!("mod with GUID {{{}}} not found", guid);
    }
}

#[tauri::command]
pub async fn add_mod(state: State<'_, AppState>, archive_file: PathBuf) -> TAResult<Mod> {
    let mut mods = state.mods.lock().await;
    if mods.is_none() {
        return anyhow::anyhow!("mods not read").into_ta_result();
    }
    let mods = mods.as_mut().unwrap();

    log::info!("Adding mod from {:?}...", archive_file);

    log::debug!("Opening archive...");
    let archive = Archive::open(&archive_file)?;

    log::debug!("Obtaining name...");
    let name = archive_file
        .file_prefix()
        .unwrap()
        .to_str()
        .map(str::to_string)
        .ok_or(anyhow::anyhow!("file name conversion failed"))?;

    log::info!("Resolving mod directory...");
    let mut mod_dir = state.base_path.join(MODS_DIRECTORY);
    mod_dir.push(&name);

    log::info!("Preparing mod directory...");
    let manifest_file = mod_dir.join(MANIFEST_FILE);
    prepare_mod_dir(mod_dir.clone(), manifest_file.clone(), name.clone()).await.into_ta_result()?;

    log::info!("Resolving manifest...");
    let (archive, manifest) = resolve_manifest(archive, name.clone(), manifest_file.clone()).await.into_ta_result()?;

    let mut r#mod = Mod {
        manifest,
        directory: mod_dir.clone(),
    };

    log::info!("Checking for duplicate...");
    if mods.iter().any(|m| m.guid() == r#mod.guid()) {
        return anyhow::anyhow!("mod with GUID {{{}}} already exists", r#mod.guid())
            .into_ta_result();
    }
    
    log::info!("Extracting archive...");
    extract_archive(archive, mod_dir).await?;

    log::debug!("Normalizing paths...");
    if let Err(e) = r#mod.normalize_paths().await {
        log::error!("Path normalization failed: {}", e);
    }

    mods.push(r#mod.clone());
    log::info!("Mod successfully added.");
    Ok(r#mod)
}

#[tauri::command]
pub async fn add_mods(state: State<'_, AppState>, archive_files: Vec<PathBuf>) -> TAResult<Vec<TAResult<Mod>>> {
    let mut mods = state.mods.lock().await;
    if mods.is_none() {
        return anyhow::anyhow!("mods not read").into_ta_result();
    }
    let mods = mods.as_mut().unwrap();

    log::info!("Adding mods from:",);
    for archive_file in &archive_files {
        log::info!(" - {:?}", archive_file);
    }
    
    log::debug!("Opening archives...");
    let data = archive_files
        .iter()
        .map(|archive_file| Archive::open(archive_file).into_ta_result())
        .collect::<Vec<_>>();

    log::debug!("Obtaining names...");
    let data = archive_files
        .iter()
        .cloned()
        .zip(data)
        .map(|(archive_file, result)| {
            result
                .zip_value(archive_file)
                .map(|(archive, archive_file)| {
                    let name = archive_file
                        .file_prefix()
                        .unwrap()
                        .to_str()
                        .map(str::to_string)
                        .ok_or(anyhow::anyhow!("file name conversion failed"))
                        .into_ta_result()?;
                    Ok((archive, name))
                })
                .flatten()
        })
        .collect::<Vec<_>>();

    log::info!("Resolving mod directories...");
    let data = data
        .into_iter()
        .map(|result| {
            result
                .map(|(archive, name)| {
                    let mut mod_dir = state.base_path.join(MODS_DIRECTORY);
                    mod_dir.push(&name);
                    let manifest_file = mod_dir.join(MANIFEST_FILE);
                    (archive, name, mod_dir, manifest_file)
                })
        })
        .collect::<Vec<_>>();

    log::info!("Preparing mod directories...");
    let data = futures::future::join_all(
        data.into_iter().map(|result| async {
            match result {
                Ok((archive, name, mod_dir, manifest_file)) => {
                    prepare_mod_dir(mod_dir.clone(), manifest_file.clone(), name.clone()).await?;
                    Ok((archive, name, mod_dir, manifest_file))
                }
                Err(e) => Err(e)
            }
        })
    ).await;

    log::info!("Resolving manifests...");
    let data = futures::future::join_all(
        data.into_iter().map(|result| async {
            match result {
                Ok((archive, name, mod_dir, manifest_file)) => {
                    let (archive, manifest) = resolve_manifest(archive, name, manifest_file).await?;
                    Ok((archive, mod_dir, manifest))
                }
                Err(e) => Err(e)
            }
        })
    ).await;
    
    let data = data
        .into_iter()
        .map(|result| {
            result.map(|(archive, mod_dir, manifest)| {
                let r#mod = Mod {
                    manifest,
                    directory: mod_dir
                };
                (archive, r#mod)
            })
        })
        .collect::<Vec<_>>();
    
    log::info!("Checking for duplicates...");
    let mut guids: HashSet<Uuid> = mods.iter().map(|m| m.guid()).collect();
    let data = data
        .into_iter()
        .map(|result| {
            result.map(|(archive, r#mod)| {
                let guid = r#mod.guid();
                if guids.insert(guid) {
                    Ok((archive, r#mod))
                } else {
                    anyhow_tauri::bail!("mod with GUID {{{}}} already exists", guid)
                }
            })
            .flatten()
        })
        .collect::<Vec<_>>();

    let mut guids = HashSet::<Uuid>::new();
    let data = data
        .into_iter()
        .map(|result| {
            result.map(|(archive, r#mod)| {
                let guid = r#mod.guid();
                if guids.insert(guid) {
                    Ok((archive, r#mod))
                } else {
                    anyhow_tauri::bail!("already adding mod with GUID {{{}}}", guid)
                }
            })
            .flatten()
        })
        .collect::<Vec<_>>();

    log::info!("Extracting archives...");
    let data = futures::future::join_all(
        data.into_iter().map(|result| async {
            match result {
                Ok((archive, r#mod)) => {
                    extract_archive(archive, r#mod.directory.clone()).await?;
                    Ok(r#mod)
                }
                Err(e) => Err(e)
            }
        })
    ).await;

    log::debug!("Normalizing paths...");
    let data = futures::future::join_all(
        data.into_iter().map(|result| async {
            match result {
                Ok(mut r#mod) => {
                    if let Err(e) = r#mod.normalize_paths().await {
                        log::error!("Path normalization failed for \"{}\": {}", r#mod.guid(), e);
                    }
                    Ok(r#mod)
                }
                Err(e) => Err(e)
            }
        })
    ).await;

    for r#mod in data.iter().flatten() {
        mods.push(r#mod.clone());
    }
    
    log::info!("Adding complete.");
    for (p, r) in archive_files.iter().zip(&data) {
        if let Err(e) = r {
            log::info!(" - {:?} : Err -> {}", p, e);
        } else {
            log::info!(" - {:?} : Ok", p);
        }
    }

    Ok(data)
}

async fn prepare_mod_dir(mod_dir: PathBuf, manifest_file: PathBuf, name: String) -> TAResult<()> {
    if tokio::fs::try_exists(&mod_dir).await.into_ta_result()? {
        if tokio::fs::try_exists(&manifest_file).await.into_ta_result()? {
            return anyhow::anyhow!("mod directory \"{}\" already exists", name).into_ta_result();
        } else {
            tokio::fs::remove_dir_all(&mod_dir).await.into_ta_result()?;
        }
    }
    tokio::fs::create_dir_all(&mod_dir).await.into_ta_result()
}

async fn resolve_manifest(mut archive: Archive, name: String, manifest_file: PathBuf) -> TAResult<(Archive, Manifest)> {
    if archive.has_path(MANIFEST_FILE)? {
        let manifest_data = archive.read_path(MANIFEST_FILE)?;
        let manifest = serde_json::from_slice(&manifest_data).into_ta_result()?;
        Ok((archive, manifest))
    } else {
        let mut guid = [0u8; 16];
        guid[..5].copy_from_slice(b"LOCAL");
        SysRng.try_fill_bytes(&mut guid[5..]).into_ta_result()?;

        let manifest = Manifest::Legacy(legacy::Manifest {
            guid: Uuid::from_bytes(guid),
            name,
            description: String::new(),
            icon_path: None,
            options: None,
        });

        let manifest_data = serde_json::to_vec_pretty(&manifest).into_ta_result()?;
        tokio::fs::write(&manifest_file, manifest_data)
            .await
            .into_ta_result()?;

        Ok((archive, manifest))
    }
}

async fn extract_archive(mut archive: Archive, mod_dir: PathBuf) -> TAResult<()> {
    tokio::task::spawn_blocking(move || archive.extract_to(mod_dir).into_ta_result()).await.into_ta_result()?
}