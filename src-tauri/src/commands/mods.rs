use std::path::{Path, PathBuf};
use anyhow_tauri::{TAResult, IntoTAResult};
use rand::{TryRng, rngs::SysRng};
use tauri::State;
use uuid::Uuid;
use crate::{AppState, archive::Archive, models::{Mod, manifest::{Manifest, legacy}}};

const MODS_DIRECTORY: &'static str = "mods/";
const MANIFEST_FILE: &'static str = "manifest.json";

#[tauri::command]
pub async fn get_mods(state: State<'_, AppState>) -> TAResult<Vec<Mod>> {
    let mods = state.mods.lock().await;

    if let Some(mods) = mods.as_ref() {
        return Ok(mods.clone());
    }

    let mods_dir = Path::new(MODS_DIRECTORY);
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

        mods.push(Mod {
            manifest,
            directory: mod_dir
        });
    }
    
    Ok(mods)
}

#[tauri::command]
pub async fn add_mod(state: State<'_, AppState>, archive_file: PathBuf) -> TAResult<Mod> {
    let mut mods = state.mods.lock().await;
    if mods.is_none() {
        return anyhow::anyhow!("mods not read").into_ta_result();
    }
    let mods = mods.as_mut().unwrap();

    let mut archive = Archive::open(&archive_file)?;
    
    let name = archive_file.file_name()
        .unwrap()
        .to_str()
        .map(str::to_string)
        .ok_or(anyhow::anyhow!("file name conversion failed"))?;

    let mut mod_dir = PathBuf::new();
    mod_dir.push(MODS_DIRECTORY);
    mod_dir.push(&name);

    let manifest_file = mod_dir.join(MANIFEST_FILE);
    if mod_dir.try_exists().into_ta_result()? {
        if tokio::fs::try_exists(&manifest_file).await.into_ta_result()? {
            return anyhow::anyhow!("mod directory \"{}\" already exists", name).into_ta_result();
        } else {
            tokio::fs::remove_dir_all(&mod_dir).await.into_ta_result()?;
        }
    }

    tokio::fs::create_dir_all(&mod_dir).await.into_ta_result()?;

    let manifest = if archive.has_path(MANIFEST_FILE)? {
        let manifest_data = archive.read_path(MANIFEST_FILE)?;
        serde_json::from_slice(&manifest_data).into_ta_result()?
    } else {
        let mut guid = [0u8; 16];
        guid[..5].copy_from_slice(b"LOCAL");
        SysRng.try_fill_bytes(&mut guid[5..]).into_ta_result()?;
        
        let manifest = Manifest::Legacy(legacy::Manifest {
            guid: Uuid::from_bytes(guid),
            name,
            description: String::new(),
            icon_path: None,
            options: None
        });

        let manifest_data = serde_json::to_vec_pretty(&manifest).into_ta_result()?;
        tokio::fs::write(&manifest_file, manifest_data).await.into_ta_result()?;

        manifest
    };

    let r#mod = Mod {
        manifest,
        directory: mod_dir.clone()
    };

    if mods.iter().any(|m| m.guid() == r#mod.guid()) {
        return anyhow::anyhow!("mod with guid {{{}}} already exists", r#mod.guid()).into_ta_result();
    }
    archive.extract_to(&mod_dir)?;

    mods.push(r#mod.clone());
    Ok(r#mod)
}