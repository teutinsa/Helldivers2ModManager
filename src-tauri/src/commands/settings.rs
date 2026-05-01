use std::path::{Path, PathBuf};

use anyhow_tauri::{IntoTAResult, TAResult};
use tauri::State;

use crate::{AppState, models::settings::Settings};

const SETTINGS_FILE: &'static str = "settings.json";

pub async fn do_load_settings(base_path: &Path) -> anyhow::Result<Settings> {
    log::info!("Loading settings...");
    let settings_file = base_path.join(SETTINGS_FILE);

    log::info!("Looking for {:?}", &settings_file);
    let settings = if tokio::fs::try_exists(&settings_file).await? {
        log::info!("Opening...");
        let data = tokio::fs::read(&settings_file).await?;

        log::info!("Deserializing...");
        serde_json::from_slice(&data)?
    } else {
        log::info!("Not found. Using default.");

        Settings::V1 {
            game_path: PathBuf::new(),
            skip_list: vec![]
        }
    };

    log::info!("Settings loaded.");
    Ok(settings)
}

pub async fn do_check_settings(base_path: &Path) -> anyhow::Result<bool> {
    log::info!("Checking settings...");

    match do_load_settings(base_path).await {
        Ok(settings) => {
            match settings.validate().await {
                Ok(()) => {
                    log::info!("Settings vaid.");
                    Ok(true)
                }
                Err(e) => {
                    log::error!("Settings invalid: {}", e);       
                    Ok(false)
                }
            }
        }
        Err(e) => Err(e)
    }
}

#[tauri::command]
pub async fn load_settings(state: State<'_, AppState>) -> TAResult<Settings> {
    do_load_settings(&state.base_path).await.into_ta_result()
}

#[tauri::command]
pub async fn save_settings(state: State<'_, AppState>, settings: Settings) -> TAResult<()> {
    log::info!("Saving settings...");

    let data = serde_json::to_vec_pretty(&settings).into_ta_result()?;
    tokio::fs::write(state.base_path.join(SETTINGS_FILE), data).await.into_ta_result()?;

    log::info!("Settings saved.");
    Ok(())
}

#[tauri::command]
pub  async fn check_settings(state: State<'_, AppState>) -> TAResult<bool> {
    do_check_settings(&state.base_path).await.into_ta_result()
}